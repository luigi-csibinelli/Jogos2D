using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Sanatorio
{
    /// <summary>
    /// "Soco em Lata", a máquina de melhorar armas no Necrotério (regras do
    /// Pack-a-Punch): 5000 pontos e precisa de energia. A arma entra na
    /// máquina, sai melhorada (dano x2, pente x1,5, reserva x2, atravessa mais
    /// um zumbi, traçante roxo) e precisa ser pega antes de afundar de volta.
    /// Uma arma melhorada não pode ser melhorada de novo.
    /// </summary>
    public class PackPunch : Interactable
    {
        private enum State { Idle, Working, Offering }

        private State state;
        private WeaponState inside;
        private SpriteRenderer machine;
        private SpriteRenderer item;
        private Light2D glow;
        private float offerUntil;

        public void Setup(SpriteRenderer machineSprite, Light2D light)
        {
            machine = machineSprite;
            glow = light;
            GameObject i = new GameObject("Item");
            i.transform.SetParent(transform, false);
            i.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            item = Res.AddSprite(i, "Gun_m1911", Order.Item + 1, false);
            item.enabled = false;
            GameEvents.PowerOn += OnPower;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEvents.PowerOn -= OnPower;
        }

        private void OnPower()
        {
            machine.sprite = Res.Sprite("PackPunch_On");
            glow.intensity = 1.2f;
        }

        public override string Prompt(Player p, out int cost)
        {
            cost = Balance.PackCost;
            if (!game.Power)
            {
                cost = -1;
                return "SOCO EM LATA - SEM ENERGIA";
            }

            switch (state)
            {
                case State.Working:
                    cost = -1;
                    return null;
                case State.Offering:
                    cost = -1;
                    return $"{Key(p)} - PEGAR {inside.Name}";
            }

            WeaponState w = p.Inventory.Current;
            if (w == null) return null;
            if (w.upgraded)
            {
                cost = -1;
                return "SOCO EM LATA - ESSA ARMA JÁ FOI MELHORADA";
            }

            return $"{Key(p)} - SOCO EM LATA: MELHORAR {w.def.name}";
        }

        public override void Use(Player p)
        {
            if (!game.Power) return;
            if (state == State.Offering)
            {
                p.Gun.CancelReload();
                p.Inventory.Return(inside);
                inside = null;
                item.enabled = false;
                state = State.Idle;
                AudioManager.Play("WeaponBuy", 1f);
                return;
            }

            WeaponState w = p.Inventory.Current;
            if (state != State.Idle || w == null || w.upgraded) return;
            if (!game.TrySpend(Balance.PackCost)) return;

            p.Gun.CancelReload();
            p.Inventory.Remove(w);
            inside = w;
            StartCoroutine(Work());
        }

        private IEnumerator Work()
        {
            state = State.Working;
            AudioManager.PlayAt("PackPunch", transform.position, 1f, 0f);
            item.sprite = Res.Sprite(inside.def.Icon);
            item.color = Color.white;
            float t = 0f;
            while (t < Balance.PackTime)
            {
                t += Time.deltaTime;
                // a máquina chacoalha e solta faíscas roxas enquanto trabalha
                machine.transform.localPosition = Random.insideUnitCircle * 0.04f;
                glow.intensity = 1.2f + Random.value * 1.2f;
                if (Random.value < 0.25f) Fx.Sparks(transform.position + Vector3.up * 0.4f, Vector2.up, 2, new Color(0.8f, 0.4f, 1f));
                yield return null;
            }

            machine.transform.localPosition = Vector3.zero;
            glow.intensity = 1.2f;
            inside.Upgrade();
            item.enabled = true;
            item.color = new Color(0.85f, 0.6f, 1f);
            state = State.Offering;
            offerUntil = Time.time + Balance.PackOfferTime;
            AudioManager.PlayAt("PackPunchDone", transform.position, 1f, 0f);
            Fx.Flash(transform.position, new Color(0.8f, 0.45f, 1f), 4f, 2f, 0.4f);

            while (state == State.Offering && Time.time < offerUntil)
            {
                // a arma afunda de volta: o tempo para pegar está acabando
                float left = (offerUntil - Time.time) / Balance.PackOfferTime;
                item.transform.localPosition = new Vector3(0f, 0.15f + 0.45f * left, 0f);
                item.enabled = left > 0.25f || Mathf.Repeat(Time.time, 0.2f) < 0.12f;
                yield return null;
            }

            if (state == State.Offering)
            {
                // não pegou a tempo: a máquina fica com a arma (como no original)
                GameEvents.RaiseToast("A MÁQUINA FICOU COM A ARMA...");
                inside = null;
                item.enabled = false;
                state = State.Idle;
            }
        }
    }
}
