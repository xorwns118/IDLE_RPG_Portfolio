#if UNITY_EDITOR
using System;
using IdleRPG.Domain;
using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Bootstrap;
using IdleRPG.Runtime.Combat;
using IdleRPG.Runtime.Configuration;
using IdleRPG.Runtime.Data;
using IdleRPG.Runtime.Maps;
using IdleRPG.Runtime.Stages;
using IdleRPG.Runtime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.Editor
{
    public static class MvpSceneSmokeTest
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/Week1VerticalSlice.unity"
        };

        [MenuItem("Idle RPG/Run MVP Scene Smoke Test")]
        public static void Run()
        {
            foreach (string scenePath in ScenePaths)
            {
                RunScene(scenePath);
            }

            RequirePrefabProfiles();
            Debug.Log("MVP scene smoke test passed.");
        }

        private static void RunScene(string _ScenePath)
        {
            EditorSceneManager.OpenScene(_ScenePath);

            MvpSceneController controller = UnityEngine.Object.FindObjectOfType<MvpSceneController>();
            if (controller == null)
            {
                GameObject controllerObject = new GameObject("MVP Scene Controller");
                controller = controllerObject.AddComponent<MvpSceneController>();
            }

            controller.RebuildSceneLayout();
            RequireSerializedReferences(controller);

            Transform root = controller.transform;
            Transform playerStartPoint = RequireTransform(root, "World/Player Start Point");
            Transform tileMapRoot = RequireTransform(root, "World/Combat Tile Map");
            Transform tiles = RequireTransform(tileMapRoot, "Tiles");
            Transform spawnPoint = RequireTransform(root, "World/Monster Spawn Point");
            Transform canvas = RequireTransform(root, "MVP HUD Canvas");
            Transform panel = RequireTransform(canvas, "Status Panel");
            Transform restartPanel = RequireTransform(canvas, "Restart Panel");

            RequireStartPoint(playerStartPoint);
            RequireMissingTransform(root, "World/Player Actor");
            RequireMissingTransform(root, "World/Monster Actor");
            RequireTileMap(tileMapRoot, tiles, playerStartPoint, spawnPoint);
            Require(spawnPoint != null, "Monster Spawn Point is missing.");

            Require(canvas.GetComponent<Canvas>() != null, "MVP HUD Canvas needs Canvas.");
            Require(canvas.GetComponent<CanvasScaler>() != null, "MVP HUD Canvas needs CanvasScaler.");
            Require(canvas.GetComponent<GraphicRaycaster>() != null, "MVP HUD Canvas needs GraphicRaycaster.");
            Require(panel.GetComponent<Image>() != null, "Status Panel needs Image.");

            RequireText(panel, "Title Text");
            RequireText(panel, "Stage Text");
            RequireText(panel, "Resource Text");
            RequireText(panel, "Player Text");
            RequireText(panel, "Enemy Text");
            RequireText(panel, "Log Text");
            RequireImage(panel, "Player HP Bar/Fill");
            RequireImage(panel, "Enemy HP Bar/Fill");
            RequireRestartPanel(restartPanel);
            RequireRuntimeBoot();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("MVP scene smoke test passed for " + _ScenePath + ".");
        }

        private static void RequireSerializedReferences(MvpSceneController _Controller)
        {
            SerializedObject serializedController = new SerializedObject(_Controller);
            RequireDesignerSettings(serializedController);
            RequireReference(serializedController, "PlayerStartPoint");
            RequireReference(serializedController, "MonsterSpawnPoint");
            RequireReference(serializedController, "HudCanvas");
            RequireReference(serializedController, "StageText");
            RequireReference(serializedController, "ResourceText");
            RequireReference(serializedController, "PlayerText");
            RequireReference(serializedController, "EnemyText");
            RequireReference(serializedController, "LogText");
            RequireReference(serializedController, "PlayerHpFill");
            RequireReference(serializedController, "EnemyHpFill");
            RequireReference(serializedController, "RestartPanel");
            RequireReference(serializedController, "RestartTitleText");
            RequireReference(serializedController, "RestartBodyText");
            RequireReference(serializedController, "RestartButton");
        }

        private static void RequireDesignerSettings(SerializedObject _SerializedObject)
        {
            SerializedProperty gameContent = RequireProperty(_SerializedObject, "GameContent");
            Require(gameContent.FindPropertyRelative("Player") != null, "GameContent needs Player settings.");
            Require(gameContent.FindPropertyRelative("Monsters") != null, "GameContent needs Monsters settings.");
            Require(gameContent.FindPropertyRelative("Stages") != null, "GameContent needs Stages settings.");

            SerializedProperty designerSettings = RequireProperty(_SerializedObject, "DesignerSettings");
            Require(designerSettings.FindPropertyRelative("Camera") != null, "DesignerSettings needs Camera settings.");
            SerializedProperty worldSettings = designerSettings.FindPropertyRelative("World");
            Require(worldSettings != null, "DesignerSettings needs World settings.");
            SerializedProperty tileMapSettings = worldSettings.FindPropertyRelative("TileMap");
            Require(tileMapSettings != null, "World settings need Tile Map settings.");
            Require(tileMapSettings.FindPropertyRelative("DefaultVisualKind") != null, "Tile Map settings need Default Visual Kind.");
            Require(tileMapSettings.FindPropertyRelative("SpritePalette") != null, "Tile Map settings need Sprite Palette.");
            Require(tileMapSettings.FindPropertyRelative("CellOverrides") != null, "Tile Map settings need Cell Overrides.");
            Require(designerSettings.FindPropertyRelative("Actors") != null, "DesignerSettings needs Actor View settings.");
            Require(designerSettings.FindPropertyRelative("Spawn") != null, "DesignerSettings needs Spawn settings.");
            Require(designerSettings.FindPropertyRelative("Hud") != null, "DesignerSettings needs HUD settings.");
            Require(designerSettings.FindPropertyRelative("RestartPanel") != null, "DesignerSettings needs Restart Panel settings.");
            Require(designerSettings.FindPropertyRelative("Stage") != null, "DesignerSettings needs Stage Runtime settings.");
        }

        private static SerializedProperty RequireProperty(SerializedObject _SerializedObject, string _PropertyName)
        {
            SerializedProperty property = _SerializedObject.FindProperty(_PropertyName);
            Require(property != null, "Serialized field is missing: " + _PropertyName);
            return property;
        }

        private static void RequireReference(SerializedObject _SerializedObject, string _PropertyName)
        {
            SerializedProperty property = _SerializedObject.FindProperty(_PropertyName);
            Require(property != null, "Serialized field is missing: " + _PropertyName);
            Require(property.objectReferenceValue != null, "Serialized field is not assigned: " + _PropertyName);
            Require(!string.IsNullOrWhiteSpace(property.objectReferenceValue.name), "Assigned object has no name: " + _PropertyName);
        }

        private static void RequireStartPoint(Transform _StartPoint)
        {
            Component[] components = _StartPoint.GetComponents<Component>();
            Require(components.Length == 1 && components[0] is Transform, "Player Start Point should only have Transform.");
            Require(_StartPoint.childCount == 0, "Player Start Point should not have preview children.");
        }

        private static void RequireRestartPanel(Transform _RestartPanel)
        {
            Require(_RestartPanel.GetComponent<Image>() != null, "Restart Panel needs Image.");
            RequireText(_RestartPanel, "Title Text");
            RequireText(_RestartPanel, "Body Text");

            Transform button = RequireTransform(_RestartPanel, "Restart Button");
            Require(button.GetComponent<Button>() != null, "Restart Button needs Button.");
            Require(button.GetComponent<Image>() != null, "Restart Button needs Image.");
            RequireText(button, "Text");
        }

        private static void RequireText(Transform _Parent, string _Path)
        {
            Transform text = RequireTransform(_Parent, _Path);
            Require(text.GetComponent<Text>() != null, _Path + " needs Text.");
        }

        private static void RequireImage(Transform _Parent, string _Path)
        {
            Transform image = RequireTransform(_Parent, _Path);
            Require(image.GetComponent<Image>() != null, _Path + " needs Image.");
        }

        private static void RequireTileMap(Transform _TileMapRoot, Transform _Tiles, Transform _PlayerStartPoint, Transform _MonsterSpawnPoint)
        {
            TileMapLayout tileMap = _TileMapRoot.GetComponent<TileMapLayout>();
            Require(tileMap != null, "Combat Tile Map needs TileMapLayout.");
            Require(tileMap.IsEnabled, "Combat Tile Map should be enabled by default.");
            Require(_TileMapRoot.GetComponent<SpriteRenderer>() == null, "Combat Tile Map should not keep the legacy ground SpriteRenderer.");

            int expectedTileCount = tileMap.Settings.Columns * tileMap.Settings.Rows;
            Require(_Tiles.childCount == expectedTileCount, "Tile count does not match Tile Map settings.");
            Require(_Tiles.Find("Tile 0,0") != null, "Tile Map needs Tile 0,0.");
            Require(_Tiles.Find("Tile " + (tileMap.Settings.Columns - 1) + "," + (tileMap.Settings.Rows - 1)) != null, "Tile Map needs the last tile.");
            Require(tileMap.Settings.CellOverrides != null, "Tile Map Cell Overrides should be initialized.");
            Require(tileMap.Settings.SpritePalette != null, "Tile Map Sprite Palette should be initialized.");
            Require(tileMap.Settings.GetSpriteSettings(TileVisualKind.Ground) != null, "Tile Map Sprite Palette needs Ground.");
            Require(tileMap.Settings.GetSpriteSettings(TileVisualKind.Wall) != null, "Tile Map Sprite Palette needs Wall.");
            Require(tileMap.Settings.GetDefaultTileKind(TileVisualKind.Wall) == TileKind.Blocked, "Wall tiles should default to blocked.");
            Require(tileMap.IsWalkable(tileMap.Settings.PlayerStartCell), "Player Start Cell should stay walkable.");
            Require(tileMap.IsWalkable(tileMap.Settings.MonsterSpawnCell), "Monster Spawn Cell should stay walkable.");
            Require(Mathf.Abs(tileMap.Settings.CellSize.x - tileMap.Settings.CellSize.y) < 0.001f, "Tile Map should use square cells.");

            Vector3 expectedPlayerStart = tileMap.CellToActorWorld(tileMap.Settings.PlayerStartCell);
            Vector3 expectedMonsterSpawn = tileMap.CellToActorWorld(tileMap.Settings.MonsterSpawnCell);
            Require(Vector3.Distance(_PlayerStartPoint.position, expectedPlayerStart) < 0.01f, "Player Start Point is not placed on the configured tile.");
            Require(Vector3.Distance(_MonsterSpawnPoint.position, expectedMonsterSpawn) < 0.01f, "Monster Spawn Point is not placed on the configured tile.");
        }

        private static void RequireRuntimeBoot()
        {
            GameObject runtimeRoot = new GameObject("Runtime Smoke Root");

            try
            {
                BattleContext context = runtimeRoot.AddComponent<BattleContext>();
                StageController stage = runtimeRoot.AddComponent<StageController>();
                Transform spawnPoint = CreateSmokeChild(runtimeRoot.transform, "Smoke Spawn Point").transform;
                MvpSceneDesignerSettings designerSettings = MvpSceneDesignerSettings.CreateDefault();
                designerSettings.World.TileMap.SetCell(new Vector2Int(2, 2), TileKind.Blocked, TileVisualKind.Wall);
                TileMapLayout tileMap = CreateSmokeChild(runtimeRoot.transform, "Smoke Tile Map").AddComponent<TileMapLayout>();
                tileMap.Configure(designerSettings.World.TileMap);
                spawnPoint.position = tileMap.CellToActorWorld(tileMap.Settings.MonsterSpawnCell);

                Vector2Int blockedCell = new Vector2Int(2, 2);
                Vector2Int nextCell = tileMap.GetNextCellToward(new Vector2Int(1, 2), new Vector2Int(3, 2), 1);
                Require(!tileMap.IsWalkable(blockedCell), "Blocked Tile Cell should not be walkable.");
                Require(tileMap.Settings.GetTileVisualKind(blockedCell) == TileVisualKind.Wall, "Blocked Tile Cell should keep its painted visual kind.");
                Require(nextCell != blockedCell, "Tile movement should avoid blocked cells.");
                Require(tileMap.IsWalkable(nextCell), "Tile movement should choose a walkable next cell.");
                Require(!designerSettings.Actors.AutoCombat.UseTileMovement, "Auto combat should use world movement by default.");

                ActorFactory factory = new ActorFactory(GeneratedSpriteFactory.CreateUnitSprite());
                stage.Initialize(new StageController.RuntimeSetup
                {
                    Database = DemoContentFactory.CreateWeek1Database(),
                    Context = context,
                    Factory = factory,
                    MonsterSpawnPoint = spawnPoint,
                    PlayerStartPosition = tileMap.CellToActorWorld(tileMap.Settings.PlayerStartCell),
                    RuntimeSettings = designerSettings.Stage,
                    ActorSettings = designerSettings.Actors,
                    SpawnSettings = designerSettings.Spawn,
                    TileMap = tileMap
                });

                Require(stage.Player != null, "StageController did not initialize the player.");
                Require(stage.ActiveMonster != null, "StageController did not initialize the first monster.");
                Require(context.TileMap == tileMap, "BattleContext did not receive the Tile Map Layout.");
                Require(stage.Player.Model.DisplayName == "Training Hero", "Player display name was not assigned.");
                Require(stage.Player.gameObject.name == "Training Hero", "Runtime player object was not created from the Hero model.");
                Require(stage.ActiveMonster.Model.DisplayName == "Slime S1", "Monster display name was not assigned.");
                RequireNameLabel(stage.Player.transform, "Training Hero");
                RequireNameLabel(stage.ActiveMonster.transform, "Slime S1");
                Require(stage.CurrentStageNumber == 1, "StageController did not start at stage 1.");
                Require(stage.RequiredKills > 0, "StageController has no required kill count.");

                Vector3 startPosition = stage.Player.transform.position;
                stage.Player.transform.position = startPosition + new Vector3(1.5f, 0f, 0f);
                stage.Player.Model.ReceiveBasicAttack(new IdleRPG.Domain.Actors.StatBlock(1f, 999f, 0f, 1f, 1f, 0f, 0f, 1f), 1f);
                Require(stage.IsPlayerDefeated, "StageController did not enter defeated state.");

                stage.RestartCurrentStage();
                Require(!stage.IsPlayerDefeated, "StageController did not clear defeated state on restart.");
                Require(stage.CurrentStageNumber == 1, "StageController changed stage number on restart.");
                Require(stage.KillsInStage == 0, "StageController did not reset kills on restart.");
                Require(stage.Player.IsAlive, "Player was not restored on restart.");
                Require(Vector3.Distance(stage.Player.transform.position, startPosition) < 0.01f, "Player did not return to the stage start point.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runtimeRoot);
            }
        }

        private static void RequirePrefabProfiles()
        {
            RequirePrefabProfile(
                "Assets/IdleRPG/Prefabs/Actors/Hero_Base.prefab",
                ActorTeam.Player,
                "player.hero",
                "Training Hero",
                140f,
                16f,
                0,
                0);

            RequirePrefabProfile(
                "Assets/IdleRPG/Prefabs/Monsters/Monster_Base.prefab",
                ActorTeam.Monster,
                "monster.base",
                "Training Monster",
                32f,
                5f,
                5,
                2);
        }

        private static void RequirePrefabProfile(
            string _PrefabPath,
            ActorTeam _ExpectedTeam,
            string _ExpectedId,
            string _ExpectedDisplayName,
            float _ExpectedMaxHp,
            float _ExpectedAttackPower,
            int _ExpectedGoldReward,
            int _ExpectedExpReward)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_PrefabPath);
            Require(prefab != null, "Missing prefab asset: " + _PrefabPath);
            Require(prefab.GetComponent<SpriteRenderer>() != null, _PrefabPath + " needs SpriteRenderer.");
            Require(prefab.GetComponent<CombatActor>() != null, _PrefabPath + " needs CombatActor.");
            Require(prefab.GetComponent<HealthBarView>() != null, _PrefabPath + " needs HealthBarView.");
            Require(prefab.GetComponent<AutoCombatController>() != null, _PrefabPath + " needs AutoCombatController.");
            Require(prefab.GetComponent<BoxCollider2D>() != null, _PrefabPath + " needs BoxCollider2D.");

            ActorPrefabProfile profile = prefab.GetComponent<ActorPrefabProfile>();
            Require(profile != null, _PrefabPath + " needs ActorPrefabProfile.");
            Require(profile.Team == _ExpectedTeam, _PrefabPath + " has wrong team.");
            Require(profile.Id == _ExpectedId, _PrefabPath + " has wrong id.");
            Require(profile.DisplayName == _ExpectedDisplayName, _PrefabPath + " has wrong display name.");
            Require(profile.Stats != null, _PrefabPath + " needs stat settings.");
            Require(Mathf.Approximately(profile.Stats.MaxHp, _ExpectedMaxHp), _PrefabPath + " has wrong Max HP.");
            Require(Mathf.Approximately(profile.Stats.AttackPower, _ExpectedAttackPower), _PrefabPath + " has wrong Attack Power.");
            Require(profile.GoldReward == _ExpectedGoldReward, _PrefabPath + " has wrong gold reward.");
            Require(profile.ExpReward == _ExpectedExpReward, _PrefabPath + " has wrong EXP reward.");

            RequireNameLabel(prefab.transform, _ExpectedDisplayName);
            RequireTransform(prefab.transform, "HP Background");
            RequireTransform(prefab.transform, "HP Fill");

            Transform anchors = RequireTransform(prefab.transform, "Effect Anchors");
            RequireTransform(anchors, "Hit Point");
            RequireTransform(anchors, "Projectile Point");
            RequireTransform(anchors, "Ground Point");
        }

        private static GameObject CreateSmokeChild(Transform _Parent, string _Name)
        {
            GameObject child = new GameObject(_Name);
            child.transform.SetParent(_Parent, false);
            return child;
        }

        private static void RequireNameLabel(Transform _Actor, string _ExpectedText)
        {
            Transform label = RequireTransform(_Actor, "Name Label");
            TextMesh textMesh = label.GetComponent<TextMesh>();
            Require(textMesh != null, "Name Label needs TextMesh.");
            Require(textMesh.text == _ExpectedText, "Name Label text mismatch. Expected: " + _ExpectedText);
            Require(label.GetComponent<MeshRenderer>() != null, "Name Label needs MeshRenderer.");
        }

        private static Transform RequireTransform(Transform _Parent, string _Path)
        {
            Transform child = _Parent.Find(_Path);
            Require(child != null, "Missing scene object: " + _Path);
            return child;
        }

        private static void RequireMissingTransform(Transform _Parent, string _Path)
        {
            Transform child = _Parent.Find(_Path);
            Require(child == null, "Scene object should be spawned at runtime, not pre-placed: " + _Path);
        }

        private static void Require(bool _Condition, string _Message)
        {
            if (!_Condition)
            {
                throw new InvalidOperationException(_Message);
            }
        }
    }
}
#endif
