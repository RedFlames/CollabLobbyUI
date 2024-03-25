using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CollabLobbyUI {

    // This entire class is an exact copy from CollabUtils2 because it currently is not accessible as a public class over there

    public class CollabMapDataProcessor : EverestMapDataProcessor {

        public struct SpeedBerryInfo
        {
            public EntityID ID;
            public float Gold;
            public float Silver;
            public float Bronze;
        }

        public struct ChapterPanelTriggerInfo {
            public string map;
            public string level;
            public string id;
            public int x, y;
        }

        public static Dictionary<string, HashSet<ChapterPanelTriggerInfo>> ChapterPanelTriggers = new Dictionary<string, HashSet<ChapterPanelTriggerInfo>>();

        // the structure here is: SilverBerries[LevelSet][SID] = ID of the silver berry in that map.
        // so, to check if all silvers in a levelset have been unlocked, go through all entries in SilverBerries[levelset].
        public static Dictionary<string, Dictionary<string, EntityID>> SilverBerries = new Dictionary<string, Dictionary<string, EntityID>>();
        public static Dictionary<string, SpeedBerryInfo> SpeedBerries = new Dictionary<string, SpeedBerryInfo>();

        private string levelName;
        private int levelX, levelY;

        public static HashSet<string> MapsWithSilverBerries = new HashSet<string>();
        public static HashSet<string> MapsWithRainbowBerries = new HashSet<string>();

        public override Dictionary<string, Action<BinaryPacker.Element>> Init()
        {
            return new Dictionary<string, Action<BinaryPacker.Element>> {
            {
                "level", level => {
                    // be sure to write the level name down.
                    levelName = level.Attr("name").Split(':')[0];
                    if (levelName.StartsWith("lvl_")) {
                        levelName = levelName.Substring(4);
                    }
                    levelX = level.AttrInt("x");
                    levelY = level.AttrInt("y");
                }
            },
            {
                "entity:CollabUtils2/SilverBerry", silverBerry => {
                    if (!SilverBerries.TryGetValue(AreaKey.GetLevelSet(), out Dictionary<string, EntityID> allSilversInLevelSet)) {
                        allSilversInLevelSet = new Dictionary<string, EntityID>();
                        SilverBerries.Add(AreaKey.GetLevelSet(), allSilversInLevelSet);
                    }
                    allSilversInLevelSet[AreaKey.GetSID()] = new EntityID(levelName, silverBerry.AttrInt("id"));

                    MapsWithSilverBerries.Add(AreaKey.GetSID());
                }
            },
            {
                "entity:CollabUtils2/RainbowBerry", berry => MapsWithRainbowBerries.Add(AreaKey.GetSID())
            },
            {
                "entity:CollabUtils2/SpeedBerry", speedBerry => {
                    SpeedBerries[AreaKey.GetSID()] = new SpeedBerryInfo() {
                        ID = new EntityID(levelName, speedBerry.AttrInt("id")),
                        Gold = speedBerry.AttrFloat("goldTime"),
                        Silver = speedBerry.AttrFloat("silverTime"),
                        Bronze = speedBerry.AttrFloat("bronzeTime")
                    };
                }
            },
            {
                "triggers", triggerList => {
                    foreach (BinaryPacker.Element trigger in triggerList.Children) {
                        if(trigger.Name == "CollabUtils2/ChapterPanelTrigger") {
                            addChapterPanelTrigger(trigger);
                        }
                    }
                }
            },
            {
                "entity:FlushelineCollab/LevelEntrance", levelEntrance => {
                    addChapterPanelTrigger(levelEntrance);
                }
            }
        };
        }

        private void addChapterPanelTrigger(BinaryPacker.Element trigger)
        {
            string map = trigger.Attr("map");
            string sid = AreaKey.GetSID();
            string trID = trigger.Attr("id");

            if (string.IsNullOrEmpty(map) || string.IsNullOrEmpty(sid)) {
                try {
                    Logger.Log(LogLevel.Warn, "CollabLobbyUI", $"Not processing trigger {sid} / {map}: {trigger.Name} / {trigger.Package} / {string.Join(",", trigger.Attributes.Keys)} / {string.Join(",", trigger.Children.Select(e => e.Name))}.");
                } catch {
                    Logger.Log(LogLevel.Warn, "CollabLobbyUI", $"Not processing trigger {sid} / {map} (failed some logging)");
                }
                return;
            }

            if (!ChapterPanelTriggers.ContainsKey(sid)) {
                ChapterPanelTriggers[sid] = new HashSet<ChapterPanelTriggerInfo>();
            }

            HashSet<ChapterPanelTriggerInfo> lobbyTriggers = ChapterPanelTriggers[sid];

            int x = levelX + trigger.AttrInt("x");
            int y = levelY + trigger.AttrInt("y");

            lobbyTriggers.Add(new ChapterPanelTriggerInfo {
                map = map,
                level = levelName,
                x = x,
                y = y,
                id = trID
            });

            Logger.Log(LogLevel.Verbose, "CollabLobbyUI", $"addChapterPanelTrigger of lobby {sid} room {levelName}: {map} at {x}/{y} with id {trID}.");
        }

        public override void Reset()
        {
            if (SilverBerries.ContainsKey(AreaKey.GetLevelSet()))
            {
                SilverBerries[AreaKey.GetLevelSet()].Remove(AreaKey.GetSID());
            }
            SpeedBerries.Remove(AreaKey.GetSID());
            MapsWithSilverBerries.Remove(AreaKey.GetSID());
            MapsWithRainbowBerries.Remove(AreaKey.GetSID());
        }

        public override void End()
        {
            // nothing to do here
        }
    }
    
}
