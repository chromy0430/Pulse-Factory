using System;
using System.Collections.Generic;
using System.IO;
using PulseFactory.Progression;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PulseFactory.Menu
{
    public static class PFLocalSave
    {
        [Serializable] private class World
        {
            public int version, pulseCount, coreLast;
            public long credits, points;
            public float elapsed;
            public List<Factory> factories = new();
            public List<Wire> wires = new();
            public List<Level> levels = new();
        }
        [Serializable] private class Factory
        {
            public string type;
            public Vector3 position;
            public Quaternion rotation;
            public int pop, paid, stored, last;
            public float charge;
        }
        [Serializable] private class Wire
        {
            public int from, to, paid;
            public PowerPortPosition output, input;
            public List<Delivery> pending = new();
        }
        [Serializable] private class Delivery { public int id; public float power, original; }
        [Serializable] private class Level { public string id; public int level; }

        public static string FilePath => Path.Combine(Application.persistentDataPath, "factory-v1.json");
        public static bool Exists => File.Exists(FilePath);
        private static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>();
        private static int Price(Component c) => c.TryGetComponent<ConstructionReceipt>(out var r) ? r.PaidCost : -1;
        public static void Save()
        {
            var state = ProgressionState.Instance;
            var timer = Find<PulseTimer>();
            var core = Find<Core>();
            var data = new World {version=1, credits=CreditWallet.Instance.CurrentCredit, points=state.ResearchPoints,
                pulseCount=timer.PulseCount, elapsed=timer.ElapsedTime, coreLast=core.LastPulseId};
            var indexes = new Dictionary<PowerNode,int> { [core]=0 };
            foreach (var n in state.catalog.nodes)
                if (state.Level(n)>0) data.levels.Add(new Level {id=n.id,level=state.Level(n)});
            foreach (var f in Find<BuildController>().BuildingRoot.GetComponentsInChildren<BuildingRuntime>())
            {
                var item = new Factory {type=f.BuildingData.Id, position=f.transform.position, rotation=f.transform.rotation,
                    pop=f.PopNumber,paid=Price(f),last=f.LastPulseId};
                if (f.TryGetComponent<CapacitorCharge>(out var cap)) {item.stored=cap.StoredCount; item.charge=cap.StoredPower;}
                data.factories.Add(item);
                indexes.Add(f,data.factories.Count);
            }
            foreach (var c in Object.FindObjectsByType<PowerCable>(FindObjectsSortMode.None))
            {
                var wire = new Wire {from=indexes[c.SourceNode],to=indexes[c.TargetNode],output=c.SourcePort.Position,
                    input=c.TargetPort.Position,paid=Price(c)};
                foreach (var pulse in c.PendingDeliveries())
                    wire.pending.Add(new Delivery {id=pulse.Id,power=pulse.Power,original=pulse.BasePower});
                data.wires.Add(wire);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            string temporary = FilePath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data,true));
            if (Exists) File.Replace(temporary,FilePath,FilePath+".bak");
            else File.Move(temporary,FilePath);
        }

        public static void Load()
        {
            var data = JsonUtility.FromJson<World>(File.ReadAllText(FilePath));
            if (data == null || data.version != 1 || data.credits < 0 || data.points < 0 ||
                data.factories == null || data.wires == null || data.levels == null) throw new InvalidDataException("저장 형식을 확인하세요.");
            var catalog = Resources.Load<BuildMenuCatalogSO>(BuildMenuCatalogSO.ResourceName);
            var defs = new Dictionary<string,BuildingDefinitionSO>();
            foreach (var d in catalog.Buildings) if (d) defs[d.Id]=d;
            foreach (var f in data.factories)
                if (!defs.ContainsKey(f.type)) throw new InvalidDataException("건물 데이터 없음: "+f.type);
            var state = ProgressionState.Instance;
            var levels = new Dictionary<string,int>();
            foreach (var l in data.levels)
            {
                var n = state.catalog.nodes.Find(x => x && x.id == l.id);
                if (!n || l.level < 0 || l.level > n.MaxLevel) throw new InvalidDataException("연구·특성 데이터가 맞지 않습니다.");
                levels.Add(l.id,l.level);
            }
            var root = Find<BuildController>().BuildingRoot;
            var connector = Find<PowerConnectionController>();
            var core = Find<Core>();
            var nodes = new List<PowerNode> {core};
            state.Restore(data.points,levels);
            core.LastPulseId=data.coreLast;
            foreach (var f in data.factories)
            {
                var d=defs[f.type];
                var go=Object.Instantiate(d.Prefab,f.position,f.rotation,root);
                var building=go.GetComponent<BuildingRuntime>();
                go.name=d.name; building.Initialize(d); building.PopNumber=f.pop; building.LastPulseId=f.last;
                if (f.paid>=0) ConstructionReceipt.Restore(go,f.paid);
                if (go.TryGetComponent<CapacitorCharge>(out var cap)) cap.Restore(f.stored,f.charge);
                nodes.Add(building);
            }
            var cables=new List<PowerCable>();
            foreach (var w in data.wires)
            {
                var a=nodes[w.from]; var b=nodes[w.to];
                var cable=connector.RestoreWire(a,Port(a,w.output),b,Port(b,w.input));
                if (!cable) throw new InvalidDataException("저장된 전선의 포트를 복원할 수 없습니다.");
                if (w.paid>=0) ConstructionReceipt.Restore(cable.gameObject,w.paid);
                cables.Add(cable);
            }
            CreditWallet.Instance.RestoreCredit(data.credits);
            Find<PulseTimer>().Restore(data.pulseCount,data.elapsed);
            for (int i=0;i<cables.Count;i++)
                foreach (var pulse in data.wires[i].pending)
                    cables[i].Transmit(PulseData.RestoreDelivery(pulse.id,pulse.power,pulse.original));
        }
        private static PowerPortSlot Port(PowerNode node, PowerPortPosition position)
        {
            foreach (var p in node.Ports) if (p.Position==position) return p;
            throw new InvalidDataException("포트 데이터가 맞지 않습니다.");
        }
    }
}
