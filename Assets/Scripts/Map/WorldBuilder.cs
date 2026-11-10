using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Sanatorio
{
    /// <summary>Referências ao que o WorldBuilder montou.</summary>
    public class World
    {
        public readonly Dictionary<Vector2Int, Barricade> Barricades = new Dictionary<Vector2Int, Barricade>();
        public readonly List<Door> Doors = new List<Door>();
        public readonly List<LightFlicker> Lamps = new List<LightFlicker>();
        public MysteryBox Box;
        public Light2D Global;
        public Rect Bounds;
        public bool AnyBarricadeBroken;

        public Barricade BarricadeAt(Vector2Int t) => Barricades.TryGetValue(t, out Barricade b) ? b : null;
    }

    /// <summary>
    /// Monta o sanatório inteiro em tempo de execução a partir da planta:
    /// tilemaps de piso/parede (com colisão), portas, janelas, armas de parede,
    /// refrigerantes, caixa, armadilhas, luzes e decoração.
    /// </summary>
    public static class WorldBuilder
    {
        private static readonly Dictionary<string, Tile> tiles = new Dictionary<string, Tile>();

        private static Tile T(string sprite, bool collide = false)
        {
            string key = sprite + (collide ? "#" : "");
            if (tiles.TryGetValue(key, out Tile t) && t != null) return t;
            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = Res.Sprite(sprite);
            t.colliderType = collide ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            tiles[key] = t;
            return t;
        }

        private static string FloorFor(int zone, int x, int y)
        {
            bool alt = ((x + y) & 1) == 0;
            switch (zone)
            {
                case 0:
                case 4: return alt ? "FloorTileA" : "FloorTileB";       // enfermarias: ladrilho
                case 1:
                case 7:
                case 6: return (y & 1) == 0 ? "FloorWoodA" : "FloorWoodB"; // varandas/corredor: tábuas
                case 2:
                case 8: return alt ? "FloorCarpetA" : "FloorCarpetB";   // andares de cima: carpete
                case 3: return "FloorConcrete";
                case 5: return alt ? "FloorMorgueA" : "FloorMorgueB";
                case 9: return alt ? "FloorKitchenA" : "FloorKitchenB";
            }

            return "FloorConcrete";
        }

        public static World Build(Game game)
        {
            MapData map = game.Map;
            World world = new World();
            Transform root = new GameObject("World").transform;

            Grid grid = new GameObject("Grid").AddComponent<Grid>();
            grid.transform.SetParent(root, false);

            Tilemap outside = MakeTilemap(grid.transform, "Outside", Order.Outside, false);
            Tilemap floor = MakeTilemap(grid.transform, "Floor", Order.Floor, false);
            Tilemap walls = MakeTilemap(grid.transform, "Walls", Order.Wall, true);

            System.Random deco = new System.Random(7);
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    Vector2Int t = new Vector2Int(x, y);
                    Vector3Int cell = map.TileToCell(t);
                    char c = map.At(t);

                    if (c == 'o')
                    {
                        int r = deco.Next(100);
                        outside.SetTile(cell, T(r < 8 ? "GrassFlower" : r < 40 ? "GrassB" : "GrassA"));
                    }
                    else if (MapData.IsWallChar(c) && c != 'W')
                    {
                        // parede com piso embaixo mostra a "face" de tijolos (falso 3/4)
                        bool face = map.IsRoom(x, y + 1) || MapData.IsDoorChar(map.At(x, y + 1));
                        walls.SetTile(cell, T(face ? "WallFace" : "WallTop", true));
                    }
                    else if (map.IsRoom(x, y))
                    {
                        string f = c == '=' ? "Stairs" : c == '~' ? "TrapGrate" : FloorFor(map.ZoneAt(t), x, y);
                        floor.SetTile(cell, T(f));
                    }
                    else if (MapData.IsDoorChar(c) || c == 'W')
                    {
                        // embaixo da porta / janela tem piso da zona vizinha
                        int z = NeighborZone(map, t);
                        floor.SetTile(cell, T(FloorFor(Mathf.Max(0, z), x, y)));
                    }
                }
            }

            walls.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();

            Transform objects = new GameObject("Objects").transform;
            objects.SetParent(root, false);

            BuildDoors(game, world, objects);
            BuildWindows(game, world, objects);
            BuildMarkers(game, world, objects);
            BuildTraps(game, world, objects);

            world.Global = LightKit.Global(root, 0.09f, new Color(0.5f, 0.55f, 0.85f));
            world.Bounds = new Rect(0, 0, map.Width, map.Height);

            GameEvents.PowerOn += () =>
            {
                world.Global.intensity = 0.42f;
                world.Global.color = new Color(1f, 0.92f, 0.8f);
                foreach (LightFlicker l in world.Lamps)
                {
                    l.on = true;
                    l.flickerChance = 0.004f;
                }
            };

            if (CameraRig.I != null) CameraRig.I.bounds = world.Bounds;
            return world;
        }

        private static int NeighborZone(MapData map, Vector2Int t)
        {
            foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                int z = map.ZoneAt(t + d);
                if (z >= 0) return z;
            }

            return -1;
        }

        private static Tilemap MakeTilemap(Transform parent, string name, int order, bool collide)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Tilemap tm = go.AddComponent<Tilemap>();
            TilemapRenderer r = go.AddComponent<TilemapRenderer>();
            r.sortingOrder = order;
            r.mode = TilemapRenderer.Mode.Chunk;
            if (Res.Lit != null) r.sharedMaterial = Res.Lit;

            if (collide)
            {
                go.layer = Layers.Wall;
                Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
                TilemapCollider2D tc = go.AddComponent<TilemapCollider2D>();
                tc.compositeOperation = Collider2D.CompositeOperation.Merge;
                CompositeCollider2D cc = go.AddComponent<CompositeCollider2D>();
                cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
            }

            return tm;
        }

        private static GameObject Solid(Transform parent, string name, Vector2 pos, string sprite, int order, Vector2 size)
        {
            GameObject go = Res.Make(name, parent, pos);
            go.layer = Layers.Wall;
            Res.AddSprite(go, sprite, order);
            BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
            bc.size = size;
            return go;
        }

        // ------------------------------------------------------------ portas
        private static void BuildDoors(Game game, World world, Transform parent)
        {
            MapData map = game.Map;
            foreach (MapData.Door d in map.Doors)
            {
                Vector2 center = Vector2.zero;
                List<GameObject> pieces = new List<GameObject>();
                foreach (Vector2Int t in d.tiles)
                {
                    Vector2 p = map.TileToWorld(t);
                    center += p;
                    pieces.Add(Solid(parent, "DoorTile", p, d.IsPowerDoor ? "DoorPower" : "Door", Order.Wall, Vector2.one));
                }

                center /= d.tiles.Count;
                GameObject go = Res.Make("Door_" + d.id, parent, center);
                Door door = go.AddComponent<Door>();
                door.Point = center;

                PixelText label = null;
                if (!d.IsPowerDoor)
                {
                    label = WorldLabel(parent, center + new Vector2(0f, 0.1f), d.cost.ToString(), new Color(1f, 0.85f, 0.35f));
                }

                door.Setup(d, pieces, label);
                world.Doors.Add(door);

                if (d.IsPowerDoor)
                {
                    GameEvents.PowerOn += door.OpenDoor;
                    Light2D warn = LightKit.Point(go.transform, center, 3f, 0.7f, new Color(0.4f, 0.6f, 1f));
                    GameEvents.PowerOn += () => { if (warn != null) warn.intensity = 0f; };
                }
            }
        }

        // ------------------------------------------------------------ janelas
        private static void BuildWindows(Game game, World world, Transform parent)
        {
            MapData map = game.Map;
            foreach (MapData.Window w in map.Windows)
            {
                Vector2 pos = map.TileToWorld(w.tile);
                GameObject go = Res.Make("Window", parent, pos);
                Res.AddSprite(go, "WindowFrame", Order.Floor + 1);

                // bloqueia só o jogador: zumbi passa quando as tábuas acabam
                GameObject block = new GameObject("PlayerBlock");
                block.transform.SetParent(go.transform, false);
                block.layer = Layers.PlayerBlock;
                block.AddComponent<BoxCollider2D>().size = Vector2.one;

                bool horizontalWall = w.outward.y != 0;
                List<SpriteRenderer> planks = new List<SpriteRenderer>();
                for (int i = 0; i < Barricade.MaxPlanks; i++)
                {
                    GameObject pl = new GameObject("Plank" + i);
                    pl.transform.SetParent(go.transform, false);
                    float off = (i - 2.5f) * 0.13f;
                    float rot = (i % 2 == 0 ? 1 : -1) * (6f + i * 3f);
                    pl.transform.localPosition = horizontalWall ? new Vector3(0f, off, 0f) : new Vector3(off, 0f, 0f);
                    pl.transform.localRotation = Quaternion.Euler(0, 0, (horizontalWall ? 0f : 90f) + rot);
                    planks.Add(Res.AddSprite(pl, "Plank", Order.Wall + 1));
                }

                Barricade b = go.AddComponent<Barricade>();
                b.Point = map.TileToWorld(w.inside);
                b.Radius = 1.3f;
                b.Setup(w, planks);
                world.Barricades[w.tile] = b;

                // luar entrando pela janela
                LightKit.Point(go.transform, pos + MapData.GridDirToWorld(-w.outward) * 0.6f, 2.2f, 0.35f, new Color(0.55f, 0.65f, 1f));
            }
        }

        // ------------------------------------------------------------ marcadores
        private static void BuildMarkers(Game game, World world, Transform parent)
        {
            MapData map = game.Map;
            List<MysteryBox.Spot> boxSpots = new List<MysteryBox.Spot>();
            int boxStart = 0;
            int noteIndex = 0;
            MapData.Marker firstToilet = null;
            foreach (MapData.Marker m in map.Markers)
            {
                if (m.c == 'L' && (firstToilet == null || m.tile.x < firstToilet.tile.x)) firstToilet = m;
            }

            foreach (MapData.Marker m in map.Markers)
            {
                Vector2 pos = map.TileToWorld(m.tile);
                Vector2 access = map.TileToWorld(m.access);
                Vector2 toRoom = (access - pos).normalized;

                if (VerrucktMap.WallBuys.TryGetValue(m.c, out string weaponId))
                {
                    WeaponDef def = WeaponCatalog.Get(weaponId);
                    GameObject go = Res.Make("WallBuy_" + weaponId, parent, pos);
                    GameObject chalkGo = new GameObject("Chalk");
                    chalkGo.transform.SetParent(go.transform, false);
                    chalkGo.transform.localPosition = toRoom * 0.18f;
                    chalkGo.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Abs(toRoom.x) > 0.5f ? 90f : 0f);
                    SpriteRenderer chalk = Res.AddSprite(chalkGo, "Chalk_" + def.id, Order.WallDecor, false);
                    chalk.color = new Color(0.95f, 0.95f, 0.88f, 0.55f);
                    WallBuy wb = go.AddComponent<WallBuy>();
                    wb.Point = access;
                    wb.Setup(def, chalk);
                    WorldLabel(parent, pos + toRoom * 0.85f + new Vector2(0, 0.05f),
                        def.wallCost.ToString(), new Color(0.95f, 0.95f, 0.85f));
                    continue;
                }

                if (VerrucktMap.Perks.TryGetValue(m.c, out PerkType perk))
                {
                    GameObject go = Solid(parent, "Perk_" + perk, pos, PerkInfo.Sprite(perk) + "_Off", Order.FloorObject, new Vector2(0.9f, 0.9f));
                    Light2D glow = LightKit.Point(go.transform, pos + toRoom * 0.4f, 2.6f, 0f, PerkInfo.Color(perk));
                    PerkMachine pm = go.AddComponent<PerkMachine>();
                    pm.Point = access;
                    pm.Setup(perk, go.GetComponent<SpriteRenderer>(), glow);
                    continue;
                }

                switch (m.c)
                {
                    case 'Z':
                    {
                        GameObject go = Solid(parent, "SocoEmLata", pos, "PackPunch_Off", Order.FloorObject, new Vector2(0.9f, 0.9f));
                        Light2D glow = LightKit.Point(go.transform, pos + toRoom * 0.4f, 3f, 0f, new Color(0.75f, 0.4f, 1f));
                        PackPunch pp = go.AddComponent<PackPunch>();
                        pp.Point = access;
                        pp.Setup(go.GetComponent<SpriteRenderer>(), glow);
                        break;
                    }
                    case 'X':
                    case '$':
                    {
                        GameObject markerGo = Res.Make("BoxSpot", parent, pos);
                        SpriteRenderer marker = Res.AddSprite(markerGo, "BoxSpot", Order.Decal + 1);
                        if (m.c == '$') boxStart = boxSpots.Count;
                        boxSpots.Add(new MysteryBox.Spot { tile = m.tile, world = pos, access = access, zone = m.zone, marker = marker });
                        break;
                    }
                    case 'P':
                    {
                        GameObject go = Res.Make("PowerSwitch", parent, pos);
                        SpriteRenderer sr = Res.AddSprite(go, "Breaker_Off", Order.WallDecor);
                        PowerSwitch ps = go.AddComponent<PowerSwitch>();
                        ps.Point = access;
                        ps.Setup(sr);
                        LightKit.Point(go.transform, access, 2f, 0.6f, new Color(0.5f, 0.7f, 1f));
                        break;
                    }
                    case '+':
                    {
                        GameObject go = Res.Make("Lamp", parent, pos);
                        SpriteRenderer sr = Res.AddSprite(go, "Lamp", Order.WallDecor, false);
                        sr.color = new Color(1f, 1f, 1f, 0.8f);
                        Light2D l = LightKit.Point(go.transform, pos, 6.5f, 0f, new Color(1f, 0.85f, 0.6f));
                        LightFlicker f = l.gameObject.AddComponent<LightFlicker>();
                        f.baseIntensity = 0.85f;
                        f.on = false;
                        world.Lamps.Add(f);
                        break;
                    }
                    case '?':
                    {
                        int idx = noteIndex++;
                        if (idx >= Lore.Notes.Length) break;
                        GameObject go = Res.Make("Note", parent, pos);
                        Res.AddSprite(go, "Note", Order.Item, false);
                        LoreNote n = go.AddComponent<LoreNote>();
                        n.index = idx;
                        n.Point = pos;
                        n.Radius = 1f;
                        LightKit.Point(go.transform, pos, 1.2f, 0.5f, new Color(1f, 0.95f, 0.7f));
                        break;
                    }
                    case 'L':
                    {
                        GameObject go = Solid(parent, "Toilet", pos, "Toilet", Order.FloorObject, new Vector2(0.7f, 0.7f));
                        Toilet t = go.AddComponent<Toilet>();
                        t.Point = access;
                        t.Radius = 1f;
                        t.secret = m == firstToilet;
                        break;
                    }
                    case '&':
                        Solid(parent, "Bed", pos, "Bed", Order.FloorObject, new Vector2(0.8f, 0.9f));
                        break;
                    case '%':
                        Solid(parent, "Table", pos, "Table", Order.FloorObject, new Vector2(0.9f, 0.8f));
                        break;
                    case '!':
                        // a alavanca é montada junto com a armadilha
                        break;
                }
            }

            Toilet.ResetSecret();

            if (boxSpots.Count > 0)
            {
                GameObject boxGo = Res.Make("MysteryBox", parent, boxSpots[boxStart].world);
                world.Box = boxGo.AddComponent<MysteryBox>();
                world.Box.Setup(boxSpots, boxStart);
            }
        }

        // ------------------------------------------------------------ armadilhas
        private static void BuildTraps(Game game, World world, Transform parent)
        {
            MapData map = game.Map;
            foreach (MapData.Trap trap in map.Traps)
            {
                Vector2 leverPos = map.TileToWorld(trap.lever);
                GameObject go = Res.Make("TrapLever", parent, leverPos);
                SpriteRenderer lever = Res.AddSprite(go, "Lever_Off", Order.WallDecor);

                List<SpriteRenderer> arcs = new List<SpriteRenderer>();
                List<Light2D> lights = new List<Light2D>();
                for (int i = 0; i < trap.tiles.Count; i++)
                {
                    Vector2 p = map.TileToWorld(trap.tiles[i]);
                    GameObject a = Res.Make("Arc", go.transform, p);
                    SpriteRenderer sr = Res.AddSprite(a, "Arc", Order.Fx - 2, false);
                    sr.enabled = false;
                    arcs.Add(sr);
                    if (i % 3 == 1) lights.Add(LightKit.Point(a.transform, p, 2.5f, 0f, new Color(0.55f, 0.75f, 1f)));
                }

                TrapLever tl = go.AddComponent<TrapLever>();
                tl.Point = map.TileToWorld(trap.leverAccess);
                tl.Setup(trap, lever, arcs, lights);
            }
        }

        // ------------------------------------------------------------ texto no mundo
        public static PixelText WorldLabel(Transform parent, Vector2 pos, string text, Color color, float scale = 1f)
        {
            GameObject cgo = new GameObject("Label");
            cgo.transform.SetParent(parent, false);
            cgo.transform.position = pos;
            Canvas canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = Order.WorldText;
            RectTransform rt = cgo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(64, 10);
            cgo.transform.localScale = Vector3.one * (scale / 16f);

            GameObject tgo = new GameObject("Text");
            tgo.transform.SetParent(cgo.transform, false);
            PixelText pt = tgo.AddComponent<PixelText>();
            pt.rectTransform.sizeDelta = new Vector2(64, 10);
            pt.align = PixelText.Align.Center;
            pt.color = color;
            pt.Text = text;
            pt.raycastTarget = false;
            return pt;
        }
    }
}
