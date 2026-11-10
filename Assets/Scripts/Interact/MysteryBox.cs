using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Sanatorio
{
    /// <summary>
    /// Caixa misteriosa: 950 pontos por uma arma aleatória. Depois de algumas
    /// rodadas de uso pode sair o ursinho: devolve os pontos e a caixa se muda.
    /// O destino segue o "ciclo da caixa" (ShuffleBag): ela passa por todos os
    /// outros pontos antes de voltar a um já visitado, pulando zonas trancadas.
    /// Sempre começa na Sala do Gerador.
    /// </summary>
    public class MysteryBox : Interactable
    {
        private enum State { Idle, Rolling, Offering, Moving }

        public class Spot
        {
            public Vector2Int tile;
            public Vector2 world;
            public Vector2 access;
            public int zone;
            public SpriteRenderer marker;   // monte de terra quando a caixa não está ali
        }

        private readonly List<Spot> spots = new List<Spot>();
        private ShuffleBag<int> bag;
        private int current;
        private State state;
        private int usesHere;
        private WeaponDef offer;
        private float offerUntil;
        private SpriteRenderer boxSprite;
        private SpriteRenderer itemSprite;
        private Light2D beam;
        private BoxCollider2D solid;

        public bool NearPlayer => game.Player != null && Vector2.Distance(game.Player.transform.position, Point) < 2.5f && state == State.Idle;
        public int CurrentSpot => current;
        public IReadOnlyList<Spot> Spots => spots;

        public void Setup(List<Spot> allSpots, int startIndex)
        {
            spots.AddRange(allSpots);
            bag = new ShuffleBag<int>(System.Linq.Enumerable.Range(0, spots.Count), game.Rng.Next(), true);
            current = startIndex;
            bag.Begin(current);

            boxSprite = Res.AddSprite(gameObject, "Box", Order.FloorObject);
            GameObject item = new GameObject("Item");
            item.transform.SetParent(transform, false);
            item.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            itemSprite = Res.AddSprite(item, "Box", Order.Item + 1, false);
            itemSprite.enabled = false;
            beam = LightKit.Point(transform, transform.position, 3.5f, 0.9f, new Color(1f, 0.85f, 0.45f));
            solid = gameObject.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(0.9f, 0.9f);
            gameObject.layer = Layers.Wall;
            MoveTo(current);
        }

        private void MoveTo(int index)
        {
            current = index;
            Spot s = spots[index];
            transform.position = s.world;
            Point = s.access;
            for (int i = 0; i < spots.Count; i++)
            {
                spots[i].marker.enabled = i != index;
            }

            usesHere = 0;
        }

        public override string Prompt(Player p, out int cost)
        {
            cost = Balance.BoxCost;
            switch (state)
            {
                case State.Idle:
                    return $"{Key(p)} - CAIXA MISTERIOSA";
                case State.Offering:
                    cost = -1;
                    return offer == null ? null : $"{Key(p)} - PEGAR {offer.name}";
            }

            return null;
        }

        public override void Use(Player p)
        {
            if (state == State.Idle)
            {
                if (!game.TrySpend(Balance.BoxCost)) return;
                StartCoroutine(Roll());
            }
            else if (state == State.Offering && offer != null)
            {
                p.Gun.CancelReload();
                p.Inventory.Give(offer);
                AudioManager.Play("WeaponBuy", 1f);
                offer = null;
                Close();
            }
        }

        private bool TeddyThisRoll()
        {
            if (usesHere < Balance.BoxSafeRolls) return false;
            // só se muda se houver outro ponto disponível (zona aberta)
            bool anyOther = false;
            for (int i = 0; i < spots.Count; i++)
            {
                if (i != current && game.Zones.IsUnlocked(spots[i].zone)) anyOther = true;
            }

            if (!anyOther) return false;
            float chance = Balance.BoxTeddyBaseChance + Balance.BoxTeddyStep * (usesHere - Balance.BoxSafeRolls);
            return game.Rng.NextDouble() < chance;
        }

        private IEnumerator Roll()
        {
            state = State.Rolling;
            usesHere++;
            bool teddy = TeddyThisRoll();
            boxSprite.sprite = Res.Sprite("BoxOpen");
            itemSprite.enabled = true;
            beam.intensity = 1.6f;
            AudioManager.PlayAt("BoxJingle", transform.position, 1f, 0f);

            // as armas giram cada vez mais devagar
            float t = 0f;
            float swap = 0f;
            float step = 0.06f;
            List<WeaponDef> pool = WeaponCatalog.All.FindAll(WeaponCatalog.InBox);
            while (t < Balance.BoxRollTime)
            {
                t += Time.deltaTime;
                swap -= Time.deltaTime;
                if (swap <= 0f)
                {
                    step = Mathf.Lerp(0.06f, 0.3f, t / Balance.BoxRollTime);
                    swap = step;
                    itemSprite.sprite = Res.Sprite(pool[game.Rng.Next(pool.Count)].Icon);
                }

                itemSprite.transform.localPosition = new Vector3(0f, 0.15f + 0.5f * (t / Balance.BoxRollTime), 0f);
                yield return null;
            }

            if (teddy)
            {
                yield return Teddy();
                yield break;
            }

            offer = WeaponCatalog.RollBox(w => game.Player.Inventory.Has(w.id), game.Rng);
            itemSprite.sprite = Res.Sprite(offer.Icon);
            state = State.Offering;
            offerUntil = Time.time + Balance.BoxOfferTime;
            AudioManager.PlayAt("BoxReveal", transform.position, 0.9f);

            while (state == State.Offering && Time.time < offerUntil)
            {
                // a arma desce de volta para a caixa: o tempo para pegar está acabando
                float left = (offerUntil - Time.time) / Balance.BoxOfferTime;
                itemSprite.transform.localPosition = new Vector3(0f, 0.15f + 0.5f * left, 0f);
                itemSprite.enabled = left > 0.25f || Mathf.Repeat(Time.time, 0.2f) < 0.12f;
                yield return null;
            }

            if (state == State.Offering)
            {
                offer = null;
                Close();
            }
        }

        private void Close()
        {
            itemSprite.enabled = false;
            boxSprite.sprite = Res.Sprite("Box");
            beam.intensity = 0.9f;
            state = State.Idle;
            AudioManager.PlayAt("BoxClose", transform.position, 0.8f);
        }

        private IEnumerator Teddy()
        {
            state = State.Moving;
            itemSprite.sprite = Res.Sprite("Teddy");
            AudioManager.PlayAt("Teddy", transform.position, 1f);
            game.AddPointsRefund(Balance.BoxCost);
            GameEvents.RaiseToast("A CAIXA VAI SE MUDAR...");

            float t = 0f;
            while (t < 1.8f)
            {
                t += Time.deltaTime;
                itemSprite.transform.localPosition = new Vector3(0f, 0.65f + t * 1.2f, 0f);
                itemSprite.color = new Color(1f, 1f, 1f, 1f - t / 1.8f);
                yield return null;
            }

            itemSprite.enabled = false;
            itemSprite.color = Color.white;
            Fx.Dust(transform.position, 20);
            boxSprite.enabled = false;
            solid.enabled = false;
            beam.intensity = 0f;
            spots[current].marker.enabled = true;

            yield return new WaitForSeconds(Balance.BoxMoveTime);

            if (bag.TryNext(current, true, i => game.Zones.IsUnlocked(spots[i].zone), out int next))
            {
                MoveTo(next);
            }
            else
            {
                MoveTo(current);
            }

            boxSprite.enabled = true;
            boxSprite.sprite = Res.Sprite("Box");
            solid.enabled = true;
            beam.intensity = 0.9f;
            Fx.Dust(transform.position, 20, new Color(1f, 0.85f, 0.5f));
            AudioManager.PlayAt("BoxLand", transform.position, 1f);
            GameEvents.RaiseToast("CAIXA EM: " + VerrucktMap.ZoneNames[spots[current].zone].ToUpperInvariant());
            state = State.Idle;
        }
    }
}
