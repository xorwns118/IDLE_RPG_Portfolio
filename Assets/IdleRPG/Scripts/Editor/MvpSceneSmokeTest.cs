#if UNITY_EDITOR
using System;
using IdleRPG.Domain;
using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Combat;
using IdleRPG.Domain.Data;
using IdleRPG.Domain.Skills;
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
            RequireNoSceneCombatActors(controller.gameObject.scene);
            RequireSerializedReferences(controller);

            Transform root = controller.transform;
            Transform playerStartPoint = RequireTransform(root, "World/Player Start Point");
            Transform tileMapRoot = RequireTransform(root, "World/Combat Tile Map");
            Transform tiles = RequireTransform(tileMapRoot, "Tiles");
            Transform spawnPoint = RequireTransform(root, "World/Monster Spawn Point");
            Transform canvas = RequireTransform(root, "MVP HUD Canvas");
            Transform panel = RequireTransform(canvas, "Status Panel");
            Transform skillPanel = RequireTransform(canvas, "Skill Panel");
            Transform restartPanel = RequireTransform(canvas, "Restart Panel");

            RequireStartPoint(playerStartPoint);
            RequireMissingTransform(root, "World/Player Actor");
            RequireMissingTransform(root, "World/Monster Actor");
            RequireTileMap(tileMapRoot, tiles, playerStartPoint, spawnPoint);
            RequireCameraFitsTileMap(tileMapRoot.GetComponent<TileMapLayout>(), controller.DesignerEditableSettings.Camera);
            RequireBattleStartTargeting(controller.DesignerEditableSettings);
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
            RequireSkillPanel(skillPanel);
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
            Require(gameContent.FindPropertyRelative("Skills") != null, "GameContent needs Skills settings.");
            Require(gameContent.FindPropertyRelative("Monsters") != null, "GameContent needs Monsters settings.");
            Require(gameContent.FindPropertyRelative("Stages") != null, "GameContent needs Stages settings.");

            SerializedProperty playerContent = gameContent.FindPropertyRelative("Player");
            Require(playerContent.FindPropertyRelative("SkillLoadout") != null, "Player settings need Skill Loadout.");
            SerializedProperty monstersContent = gameContent.FindPropertyRelative("Monsters");
            Require(monstersContent.isArray, "Monster settings should be an array.");
            if (monstersContent.arraySize > 0)
            {
                SerializedProperty firstMonster = monstersContent.GetArrayElementAtIndex(0);
                Require(firstMonster.FindPropertyRelative("SkillLoadout") != null, "Monster settings need Skill Loadout.");
            }

            SerializedProperty designerSettings = RequireProperty(_SerializedObject, "DesignerSettings");
            SerializedProperty cameraSettings = designerSettings.FindPropertyRelative("Camera");
            Require(cameraSettings != null, "DesignerSettings needs Camera settings.");
            Require(cameraSettings.FindPropertyRelative("AutoFitTileMap") != null, "Camera settings need Auto Fit Tile Map.");
            Require(cameraSettings.FindPropertyRelative("ReferenceMaxColumns") != null, "Camera settings need Reference Max Columns.");
            Require(cameraSettings.FindPropertyRelative("ReferenceMaxRows") != null, "Camera settings need Reference Max Rows.");
            Require(cameraSettings.FindPropertyRelative("TileMapPaddingCells") != null, "Camera settings need Tile Map Padding Cells.");
            SerializedProperty worldSettings = designerSettings.FindPropertyRelative("World");
            Require(worldSettings != null, "DesignerSettings needs World settings.");
            SerializedProperty tileMapSettings = worldSettings.FindPropertyRelative("TileMap");
            Require(tileMapSettings != null, "World settings need Tile Map settings.");
            Require(tileMapSettings.FindPropertyRelative("MonsterSpawnCells") != null, "Tile Map settings need Monster Spawn Cells.");
            Require(tileMapSettings.FindPropertyRelative("DefaultVisualKind") != null, "Tile Map settings need Default Visual Kind.");
            Require(tileMapSettings.FindPropertyRelative("SpritePalette") != null, "Tile Map settings need Sprite Palette.");
            Require(tileMapSettings.FindPropertyRelative("CellOverrides") != null, "Tile Map settings need Cell Overrides.");
            SerializedProperty actorSettings = designerSettings.FindPropertyRelative("Actors");
            Require(actorSettings != null, "DesignerSettings needs Actor View settings.");
            SerializedProperty animationSettings = actorSettings.FindPropertyRelative("Animation");
            Require(animationSettings != null, "Actor View settings need Animation settings.");
            Require(animationSettings.FindPropertyRelative("MirrorSpriteRendererByFacing") != null, "Animation settings need Mirror Sprite Renderer By Facing.");
            Require(!animationSettings.FindPropertyRelative("MirrorSpriteRendererByFacing").boolValue, "Directional animation clips should not also mirror SpriteRenderer by default.");
            SerializedProperty autoCombatSettings = actorSettings.FindPropertyRelative("AutoCombat");
            Require(autoCombatSettings != null, "Actor View settings need Auto Combat settings.");
            Require(autoCombatSettings.FindPropertyRelative("SkillUseDelaySeconds") != null, "Auto Combat settings need Skill Use Delay Seconds.");
            Require(autoCombatSettings.FindPropertyRelative("SkillReadyDelaySeconds") != null, "Auto Combat settings need Skill Ready Delay Seconds.");
            SerializedProperty targetingSettings = actorSettings.FindPropertyRelative("Targeting");
            Require(targetingSettings != null, "Actor View settings need Targeting settings.");
            Require(targetingSettings.FindPropertyRelative("LimitSearchRange") != null, "Targeting settings need Limit Search Range.");
            Require(targetingSettings.FindPropertyRelative("SearchRange") != null, "Targeting settings need Search Range.");
            SerializedProperty combatLoopSettings = designerSettings.FindPropertyRelative("CombatLoop");
            Require(combatLoopSettings != null, "DesignerSettings needs Combat Loop settings.");
            Require(combatLoopSettings.FindPropertyRelative("Mode") != null, "Combat Loop settings need Mode.");
            SerializedProperty spawnSettings = designerSettings.FindPropertyRelative("Spawn");
            Require(spawnSettings != null, "DesignerSettings needs Spawn settings.");
            Require(spawnSettings.FindPropertyRelative("SpawnCells") != null, "Spawn settings need Spawn Cells.");
            Require(designerSettings.FindPropertyRelative("SceneFlow") != null, "DesignerSettings needs Scene Flow settings.");
            Require(designerSettings.FindPropertyRelative("FieldEncounter") != null, "DesignerSettings needs Field Encounter settings.");
            SerializedProperty turnCombatSettings = designerSettings.FindPropertyRelative("TurnCombat");
            Require(turnCombatSettings != null, "DesignerSettings needs Turn Combat settings.");
            Require(turnCombatSettings.FindPropertyRelative("UseTileMovement") != null, "Turn Combat settings need Use Tile Movement.");
            Require(turnCombatSettings.FindPropertyRelative("SkillUseDelaySeconds") != null, "Turn Combat settings need Skill Use Delay Seconds.");
            Require(turnCombatSettings.FindPropertyRelative("SkillReadyDelaySeconds") != null, "Turn Combat settings need Skill Ready Delay Seconds.");
            Require(turnCombatSettings.FindPropertyRelative("WorldMoveSecondsPerTurn") != null, "Turn Combat settings need World Move Seconds Per Turn.");
            SerializedProperty hudSettings = designerSettings.FindPropertyRelative("Hud");
            Require(hudSettings != null, "DesignerSettings needs HUD settings.");
            SerializedProperty skillUiSettings = hudSettings.FindPropertyRelative("SkillUi");
            Require(skillUiSettings != null, "HUD settings need Skill UI settings.");
            Require(skillUiSettings.FindPropertyRelative("Enabled") != null, "Skill UI settings need Enabled.");
            Require(skillUiSettings.FindPropertyRelative("CooldownDisplayStepSeconds") != null, "Skill UI settings need Cooldown Display Step Seconds.");
            Require(skillUiSettings.FindPropertyRelative("PanelPosition") != null, "Skill UI settings need Panel Position.");
            Require(skillUiSettings.FindPropertyRelative("SlotSize") != null, "Skill UI settings need Slot Size.");
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

        private static void RequireSkillPanel(Transform _SkillPanel)
        {
            Require(_SkillPanel.GetComponent<Image>() != null, "Skill Panel needs Image.");
            RequireText(_SkillPanel, "Title Text");

            for (int i = 1; i <= SkillLoadout.MaxSlots; i++)
            {
                Transform slot = RequireTransform(_SkillPanel, "Skill Slot " + i);
                Require(slot.GetComponent<Image>() != null, "Skill Slot " + i + " needs Image.");
                RequireImage(slot, "Cooldown Fill");
                RequireText(slot, "Skill Name Text");
                RequireText(slot, "Cooldown Text");
            }
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
            Require(tileMap.Settings.MonsterSpawnCellCount >= 1, "Tile Map needs at least one Monster Spawn Cell.");
            Vector2Int[] monsterSpawnCells = tileMap.Settings.GetMonsterSpawnCells();
            for (int i = 0; i < monsterSpawnCells.Length; i++)
            {
                Require(tileMap.Settings.CanUseMonsterSpawnCell(monsterSpawnCells[i]), "Monster Spawn Cell should be a walkable non-player-start cell.");
            }

            Require(Mathf.Abs(tileMap.Settings.CellSize.x - tileMap.Settings.CellSize.y) < 0.001f, "Tile Map should use square cells.");

            Vector3 expectedPlayerStart = tileMap.CellToActorWorld(tileMap.Settings.PlayerStartCell);
            Vector3 expectedMonsterSpawn = tileMap.CellToActorWorld(tileMap.Settings.GetPrimaryMonsterSpawnCell());
            Require(Vector3.Distance(_PlayerStartPoint.position, expectedPlayerStart) < 0.01f, "Player Start Point is not placed on the configured tile.");
            Require(Vector3.Distance(_MonsterSpawnPoint.position, expectedMonsterSpawn) < 0.01f, "Monster Spawn Point is not placed on the configured tile.");
        }

        private static void RequireCameraFitsTileMap(TileMapLayout _TileMap, MvpCameraSettings _CameraSettings)
        {
            Camera camera = Camera.main;
            Require(camera != null, "Main camera is missing.");
            Require(camera.orthographic, "Main camera should be orthographic.");
            Require(_CameraSettings.AutoFitTileMap, "Camera should auto fit the tile map by default.");

            Bounds mapBounds = _TileMap.GetWorldBounds();
            Vector2 cameraCenter = new Vector2(camera.transform.position.x, camera.transform.position.y);
            Vector2 mapCenter = new Vector2(mapBounds.center.x, mapBounds.center.y);
            float expectedSize = _CameraSettings.CalculateTileMapOrthographicSize(_TileMap.Settings, camera.aspect);

            Require(Vector2.Distance(cameraCenter, mapCenter) < 0.01f, "Main camera is not centered on the tile map.");
            Require(Mathf.Abs(camera.orthographicSize - expectedSize) < 0.01f, "Main camera size does not match the fitted tile map size.");
        }

        private static void RequireBattleStartTargeting(MvpSceneDesignerSettings _Settings)
        {
            MvpTargetingSettings targeting = _Settings.Actors.Targeting;
            targeting.EnsureDefaults();
            Require(!targeting.LimitSearchRange || GetStartDistance(_Settings.World) <= targeting.SearchRange, "Battle start positions are outside Targeting Search Range.");
        }

        private static float GetStartDistance(MvpWorldLayoutSettings _WorldSettings)
        {
            if (_WorldSettings.TileMap.Enabled)
            {
                MvpTileMapSettings tileMap = _WorldSettings.TileMap;
                Vector3 playerPosition = tileMap.CellToLocal(tileMap.PlayerStartCell) + tileMap.ActorAnchorOffset;
                Vector3 monsterPosition = tileMap.CellToLocal(tileMap.GetPrimaryMonsterSpawnCell()) + tileMap.ActorAnchorOffset;
                return Vector2.Distance(playerPosition, monsterPosition);
            }

            return Vector2.Distance(_WorldSettings.PlayerStartPosition, _WorldSettings.MonsterSpawnPosition);
        }

        private static void RequireRuntimeBoot()
        {
            GameObject runtimeRoot = new GameObject("Runtime Smoke Root");
            StageController stage = null;

            try
            {
                BattleContext context = runtimeRoot.AddComponent<BattleContext>();
                stage = runtimeRoot.AddComponent<StageController>();
                Transform spawnPoint = CreateSmokeChild(runtimeRoot.transform, "Smoke Spawn Point").transform;
                MvpSceneDesignerSettings designerSettings = MvpSceneDesignerSettings.CreateDefault();
                TileMapLayout tileMap = CreateSmokeChild(runtimeRoot.transform, "Smoke Tile Map").AddComponent<TileMapLayout>();
                tileMap.Configure(designerSettings.World.TileMap);

                System.Collections.Generic.List<Vector2Int> straightPath = new System.Collections.Generic.List<Vector2Int>();
                Require(
                    tileMap.TryGetPathToward(new Vector2Int(1, 2), new Vector2Int(5, 2), 1, straightPath)
                    && straightPath.Count == 3
                    && straightPath[0] == new Vector2Int(2, 2)
                    && straightPath[1] == new Vector2Int(3, 2)
                    && straightPath[2] == new Vector2Int(4, 2),
                    "A* tile movement should keep a straight path when no obstacle is blocking it.");

                designerSettings.World.TileMap.SetCell(new Vector2Int(2, 2), TileKind.Blocked, TileVisualKind.Wall);
                designerSettings.World.TileMap.AddMonsterSpawnCell(new Vector2Int(6, 3));
                designerSettings.World.TileMap.AddMonsterSpawnCell(new Vector2Int(2, 2));
                spawnPoint.position = tileMap.CellToActorWorld(tileMap.Settings.GetPrimaryMonsterSpawnCell());

                Vector2Int blockedCell = new Vector2Int(2, 2);
                Vector2Int nextCell = tileMap.GetNextCellToward(new Vector2Int(1, 2), new Vector2Int(3, 2), 1);
                Require(!tileMap.IsWalkable(blockedCell), "Blocked Tile Cell should not be walkable.");
                Require(tileMap.Settings.GetTileVisualKind(blockedCell) == TileVisualKind.Wall, "Blocked Tile Cell should keep its painted visual kind.");
                Require(nextCell != blockedCell, "Tile movement should avoid blocked cells.");
                Require(tileMap.IsWalkable(nextCell), "Tile movement should choose a walkable next cell.");
                Require(tileMap.Settings.HasMultipleMonsterSpawnCells, "Tile Map should support multiple Monster Spawn Cells.");
                Require(tileMap.Settings.IsMonsterSpawnCell(new Vector2Int(6, 3)), "Tile Map did not keep the added Monster Spawn Cell.");
                Require(!tileMap.Settings.IsMonsterSpawnCell(blockedCell), "Blocked Tile Cell should not become a Monster Spawn Cell.");
                Require(designerSettings.Actors.AutoCombat.UseTileMovement, "Auto combat should use tile movement by default.");
                Require(!designerSettings.Actors.Targeting.LimitSearchRange, "Target search range should not stop battle startup by default.");
                Require(!designerSettings.FieldEncounter.Enabled, "Field encounter should be opt-in for the current battle MVP scene.");
                Require(designerSettings.CombatLoop.Mode == CombatLoopMode.Realtime, "Realtime combat should be the default MVP combat loop.");
                RequireStateAndStatModifier();
                RequireAnimationFacingPolicy();
                RequireSkillSystem();

                ActorFactory factory = new ActorFactory(GeneratedSpriteFactory.CreateUnitSprite(), designerSettings.Actors, designerSettings.CombatLoop.Mode);
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
                Require(context.FindTarget(stage.Player) == stage.ActiveMonster, "BattleContext targeting did not select the active monster.");
                Require(stage.Player.Model.DisplayName == "Training Hero", "Player display name was not assigned.");
                Require(stage.Player.gameObject.name == "Training Hero", "Runtime player object was not created from the Hero model.");
                Require(stage.ActiveMonster.Model.DisplayName == "Slime S1", "Monster display name was not assigned.");
                Require(stage.Player.Model.SkillLoadout.HasAnySkill, "Player did not receive a skill loadout.");
                Require(stage.ActiveMonster.Model.SkillLoadout.HasAnySkill, "Monster did not receive a skill loadout.");
                RequireNameLabel(stage.Player.transform, "Training Hero");
                RequireNameLabel(stage.ActiveMonster.transform, "Slime S1");
                Require(stage.CurrentStageNumber == 1, "StageController did not start at stage 1.");
                Require(stage.RequiredKills > 0, "StageController has no required kill count.");
                Vector3 stageStartPosition = tileMap.CellToActorWorld(tileMap.Settings.PlayerStartCell);
                Require(Vector3.Distance(stage.Player.transform.position, stageStartPosition) < 0.01f, "Player did not start on the configured tile.");
                RequireCombatLoopSelection(stage, context);
                RequireCombatPolicies(runtimeRoot, context, stage);
                RequireSceneFlow(runtimeRoot);

                stage.Player.transform.position = stageStartPosition + new Vector3(1.5f, 0f, 0f);
                stage.Player.Model.ReceiveBasicAttack(new IdleRPG.Domain.Actors.StatBlock(1f, 999f, 0f, 1f, 1f, 0f, 0f, 1f), 1f);
                Require(stage.IsPlayerDefeated, "StageController did not enter defeated state.");

                stage.RestartCurrentStage();
                Require(!stage.IsPlayerDefeated, "StageController did not clear defeated state on restart.");
                Require(stage.CurrentStageNumber == 1, "StageController changed stage number on restart.");
                Require(stage.KillsInStage == 0, "StageController did not reset kills on restart.");
                Require(stage.Player.IsAlive, "Player was not restored on restart.");
                Require(Vector3.Distance(stage.Player.transform.position, stageStartPosition) < 0.01f, "Player did not return to the stage start point.");
            }
            finally
            {
                if (stage != null)
                    stage.ClearRuntime();

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

        private static void RequireStateAndStatModifier()
        {
            ActorModel actor = new ActorModel(
                "smoke.actor",
                "Smoke Actor",
                ActorTeam.Player,
                new StatBlock(100f, 10f, 2f, 1f, 1f, 2f, 0.1f, 1.5f));

            actor.SetState(ActorState.Move);
            Require(actor.State == ActorState.Move, "Actor state machine did not accept Move state.");

            actor.ApplyStatModifier(StatModifier.Additive(_MaxHp: 50f, _AttackPower: 5f));
            Require(Mathf.Approximately(actor.Stats.MaxHp, 150f), "Stat modifier did not apply Max HP.");
            Require(Mathf.Approximately(actor.Stats.AttackPower, 15f), "Stat modifier did not apply Attack Power.");
            Require(Mathf.Approximately(actor.CurrentHp, 150f), "Stat modifier did not preserve HP percentage.");

            actor.AddStatModifier("smoke.buff", StatModifier.Multiplier(_AttackPower: 2f), _DurationSeconds: 1f);
            Require(actor.ActiveStatModifierCount == 2, "Actor did not add a stacked stat modifier.");
            Require(Mathf.Approximately(actor.Stats.AttackPower, 30f), "Stacked stat modifier did not multiply Attack Power.");
            actor.TickStatModifiers(2f);
            Require(actor.ActiveStatModifierCount == 1, "Actor did not remove expired stat modifiers.");

            ActorModel combatActor = new ActorModel(
                "smoke.combat",
                "Smoke Combat Actor",
                ActorTeam.Player,
                new StatBlock(100f, 10f, 2f, 1f, 1f, 2f, 0.1f, 1.5f));
            Require(!combatActor.IsInCombat, "Actor should start outside combat.");
            combatActor.ReceiveBasicAttack(new StatBlock(1f, 10f, 0f, 1f, 1f, 0f, 0f, 1f), 1f);
            Require(combatActor.IsInCombat, "Actor should enter combat after being hit.");
            combatActor.RestoreFull();
            Require(!combatActor.IsInCombat, "RestoreFull should clear combat state.");

            actor.ReceiveBasicAttack(new StatBlock(1f, 999f, 0f, 1f, 1f, 0f, 0f, 1f), 1f);
            Require(actor.State == ActorState.Dead, "Actor state machine did not enter Dead state.");
            Require(!actor.IsInCombat, "Dead actor should leave combat.");
            actor.SetState(ActorState.Move);
            Require(actor.State == ActorState.Dead, "Actor state machine allowed Dead to return to Move.");

            actor.RestoreFull();
            actor.ClearStatModifier();
            Require(actor.State == ActorState.Idle, "Actor state machine did not reset on restore.");
            Require(Mathf.Approximately(actor.Stats.MaxHp, 100f), "Stat modifier did not clear.");
        }

        private static void RequireSkillSystem()
        {
            RuntimeContentDatabase database = MvpGameContentSettings.CreateDefault().CreateDatabase();
            Require(database.Skills.Count >= 3, "Default content should include skill definitions.");
            Require(database.Player.SkillLoadout.Count >= 2, "Default player should include a skill loadout.");
            Require(database.Player.SkillLoadout.Count <= SkillLoadout.MaxSlots, "Player loadout exceeded four skill slots.");
            Require(database.GetMonster("monster.slime").SkillLoadout.Count >= 1, "Default monster should include a skill loadout.");

            SkillDefinition damageSkill = database.GetSkill("skill.hero.power_strike");
            SkillDefinition buffSkill = database.GetSkill("skill.hero.battle_focus");
            SkillDefinition monsterSkill = database.GetSkill("skill.monster.bite");
            SkillLoadout clippedLoadout = new SkillLoadout(new[]
            {
                damageSkill,
                buffSkill,
                monsterSkill,
                damageSkill,
                buffSkill
            });
            Require(clippedLoadout.Slots.Count == SkillLoadout.MaxSlots, "Skill loadout should expose four slots.");
            Require(clippedLoadout.FilledSlotCount == SkillLoadout.MaxSlots, "Skill loadout did not clamp to four filled slots.");

            ActorModel caster = new ActorModel(
                "skill.caster",
                "Skill Caster",
                ActorTeam.Player,
                new StatBlock(100f, 10f, 0f, 1f, 1f, 1f, 0f, 1f));
            ActorModel target = new ActorModel(
                "skill.target",
                "Skill Target",
                ActorTeam.Monster,
                new StatBlock(100f, 5f, 0f, 1f, 1f, 1f, 0f, 1f));
            SkillExecutor executor = new SkillExecutor();
            ISkillExecutor modelExecutor = executor;

            SkillRuntime preCombatRuntime = new SkillRuntime(damageSkill);
            SkillExecutionResult preCombatResult = modelExecutor.Execute(preCombatRuntime, caster, target, damageSkill.Range, 1f);
            Require(!preCombatResult.Succeeded, "Skill should not execute before combat engagement.");

            caster.EnterCombat();
            target.EnterCombat();
            SkillRuntime damageRuntime = new SkillRuntime(damageSkill);
            float targetHpBefore = target.CurrentHp;
            SkillExecutionResult damageResult = modelExecutor.Execute(damageRuntime, caster, target, damageSkill.Range, 1f);
            Require(damageResult.Succeeded, "Damage skill did not execute.");
            Require(target.CurrentHp < targetHpBefore, "Damage skill did not reduce target HP.");
            Require(!damageRuntime.IsReady, "Damage skill did not start cooldown.");
            damageRuntime.Tick(damageSkill.CooldownSeconds);
            Require(damageRuntime.IsReady, "Damage skill cooldown did not recover.");

            SkillRuntime buffRuntime = new SkillRuntime(buffSkill);
            float attackBefore = caster.Stats.AttackPower;
            SkillExecutionResult buffResult = modelExecutor.Execute(buffRuntime, caster, target, buffSkill.Range, 1f);
            Require(buffResult.Succeeded, "Buff skill did not execute.");
            Require(caster.Stats.AttackPower > attackBefore, "Buff skill did not increase caster Attack Power.");
            caster.Tick(buffSkill.Effects[0].DurationSeconds + 0.1f);
            Require(Mathf.Approximately(caster.Stats.AttackPower, attackBefore), "Buff skill did not expire.");

            SkillRuntime farBuffRuntime = new SkillRuntime(buffSkill);
            SkillExecutionResult farBuffResult = modelExecutor.Execute(farBuffRuntime, caster, target, buffSkill.Range + 20f, 1f);
            Require(farBuffResult.Succeeded, "Self skill should execute without target distance gating while in combat.");

            RequireRuntimeSkillEvent(damageSkill);
        }

        private static void RequireRuntimeSkillEvent(SkillDefinition _Skill)
        {
            GameObject root = new GameObject("Runtime Skill Smoke Root");
            try
            {
                Sprite sprite = GeneratedSpriteFactory.CreateUnitSprite();
                GameObject casterObject = CreateSmokeChild(root.transform, "Runtime Skill Caster");
                casterObject.AddComponent<SpriteRenderer>();
                CombatActor caster = casterObject.AddComponent<CombatActor>();
                ActorModel casterModel = new ActorModel(
                    "runtime.skill.caster",
                    "Runtime Skill Caster",
                    ActorTeam.Player,
                    new StatBlock(100f, 10f, 0f, 1f, 1f, 1f, 0f, 1f));
                casterModel.SetSkillLoadout(new SkillLoadout(new[] { _Skill }));
                caster.Initialize(casterModel, sprite, Color.white);

                GameObject targetObject = CreateSmokeChild(root.transform, "Runtime Skill Target");
                targetObject.transform.position = Vector3.right * 0.5f;
                targetObject.AddComponent<SpriteRenderer>();
                CombatActor target = targetObject.AddComponent<CombatActor>();
                ActorModel targetModel = new ActorModel(
                    "runtime.skill.target",
                    "Runtime Skill Target",
                    ActorTeam.Monster,
                    new StatBlock(100f, 5f, 0f, 1f, 1f, 1f, 0f, 1f));
                target.Initialize(targetModel, sprite, Color.white);

                bool damageEventRaised = false;
                target.DamageTaken += (_Target, _Attacker, _Result) =>
                {
                    damageEventRaised = _Target == target && _Attacker == caster && _Result.FinalDamage > 0f;
                };

                bool skillEventRaised = false;
                caster.SkillUsed += (_Caster, _Target, _Result) =>
                {
                    skillEventRaised = _Caster == caster && _Target == target && _Result.Succeeded && _Result.SkillId == _Skill.Id;
                };

                SkillExecutor executor = new SkillExecutor();
                Require(
                    !executor.TryExecuteBestSkill(caster, target, 0.5f, 1f, out SkillExecutionResult preCombatResult) && !preCombatResult.Succeeded,
                    "Runtime SkillExecutor should wait until the caster is in combat.");

                target.TakeBasicAttack(caster, 1f);
                Require(caster.IsInCombat && target.IsInCombat, "Basic attack should put both runtime actors in combat.");
                damageEventRaised = false;
                SkillReadinessGate readinessGate = new SkillReadinessGate();
                Require(
                    !executor.TryExecuteBestSkill(caster, target, 0.5f, 1f, readinessGate, 1f, out SkillExecutionResult readyDelayResult) && !readyDelayResult.Succeeded,
                    "Runtime SkillExecutor should wait after a skill becomes ready.");
                readinessGate.Tick(0.5f);
                Require(
                    !executor.TryExecuteBestSkill(caster, target, 0.5f, 1f, readinessGate, 1f, out SkillExecutionResult partialReadyDelayResult)
                    && !partialReadyDelayResult.Succeeded,
                    "Runtime SkillExecutor should keep waiting until the ready delay finishes.");
                readinessGate.Tick(0.5f);
                Require(
                    executor.TryExecuteBestSkill(caster, target, 0.5f, 1f, readinessGate, 1f, out SkillExecutionResult result) && result.Succeeded,
                    "Runtime SkillExecutor did not execute after the ready delay.");
                Require(damageEventRaised, "Runtime skill damage did not raise DamageTaken.");
                Require(skillEventRaised, "Runtime skill execution did not raise SkillUsed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void RequireAnimationFacingPolicy()
        {
            GameObject actorObject = new GameObject("Animation Facing Smoke");
            try
            {
                SpriteRenderer spriteRenderer = actorObject.AddComponent<SpriteRenderer>();
                Animator animator = actorObject.AddComponent<Animator>();
                ActorAnimationView animationView = actorObject.AddComponent<ActorAnimationView>();
                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/IdleRPG/Animations/Default_Character.overrideController");
                AnimationClip idleRightClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/IdleRPG/Animations/Link_Idle_Right.anim");
                Require(controller != null, "Default animation override controller is missing.");
                RequireIdleRightClip(idleRightClip);

                animator.runtimeAnimatorController = controller;
                animationView.Configure(controller, new MvpActorAnimationSettings
                {
                    MirrorSpriteRendererByFacing = false
                });

                spriteRenderer.flipX = false;
                animationView.Face(Vector3.left);
                Require(animator.GetBool("IsLeft"), "Facing left did not set IsLeft.");
                Require(!spriteRenderer.flipX, "Directional animation should not also flip SpriteRenderer.");

                animationView.PlayMovement(new Vector3(-0.2f, -0.2f, 0f), Vector3.right);
                Require(animator.GetBool("IsLeft"), "Left-down movement should use the left walk animation.");
                Require(animator.GetBool("IsWalk"), "Movement should set IsWalk.");
                Require(!spriteRenderer.flipX, "Left-down movement should not flip a directional animation clip.");

                animationView.PlayMovement(new Vector3(0.2f, 0f, 0f), Vector3.left);
                Require(!animator.GetBool("IsLeft"), "Right movement should use the right walk animation even when the target point is left.");
                Require(animator.GetBool("IsWalk"), "Right movement should keep IsWalk.");

                animationView.PlayIdle();
                Require(!animator.GetBool("IsWalk"), "Idle animation should clear IsWalk.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actorObject);
            }
        }

        private static void RequireIdleRightClip(AnimationClip _IdleRightClip)
        {
            Require(_IdleRightClip != null, "Idle right animation clip is missing.");
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(_IdleRightClip);
            Require(bindings.Length == 1, "Idle right animation should only bind SpriteRenderer.m_Sprite.");
            ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(_IdleRightClip, bindings[0]);
            Require(keyframes.Length == 1, "Idle right animation should use one right-facing sprite.");

            Sprite sprite = keyframes[0].value as Sprite;
            Require(sprite != null && sprite.name == "link_7", "Idle right animation should use the right-facing link_7 sprite.");
        }

        private static void RequireCombatLoopSelection(StageController _Stage, BattleContext _Context)
        {
            AutoCombatController realtimeLoop = _Stage.Player.GetComponent<AutoCombatController>();
            AutoCombatController realtimeMonsterLoop = _Stage.ActiveMonster.GetComponent<AutoCombatController>();
            Require(realtimeLoop != null, "Realtime actor loop is missing.");
            Require(realtimeMonsterLoop != null, "Realtime monster loop is missing.");
            Require(realtimeLoop.IsRuntimeActive, "Realtime actor loop should be active in Realtime mode.");
            Require(realtimeMonsterLoop.IsRuntimeActive, "Realtime monster loop should be active in Realtime mode.");

            _Stage.SetRealtimeCombatActive(false);
            Require(!_Stage.IsRealtimeCombatActive, "StageController did not store inactive realtime combat state.");
            Require(!realtimeLoop.IsRuntimeActive, "Realtime actor loop did not stop when StageController disabled realtime combat.");
            Require(!realtimeMonsterLoop.IsRuntimeActive, "Realtime monster loop did not stop when StageController disabled realtime combat.");

            _Stage.SetRealtimeCombatActive(true);
            Require(_Stage.IsRealtimeCombatActive, "StageController did not store active realtime combat state.");
            Require(realtimeLoop.IsRuntimeActive, "Realtime actor loop did not resume when StageController enabled realtime combat.");
            Require(realtimeMonsterLoop.IsRuntimeActive, "Realtime monster loop did not resume when StageController enabled realtime combat.");

            GameObject loopRoot = new GameObject("Combat Loop Selection Smoke Root");
            try
            {
                BattleContext turnContext = loopRoot.AddComponent<BattleContext>();
                MvpSceneDesignerSettings designerSettings = MvpSceneDesignerSettings.CreateDefault();
                ActorFactory turnFactory = new ActorFactory(
                    GeneratedSpriteFactory.CreateUnitSprite(),
                    designerSettings.Actors,
                    CombatLoopMode.TurnBased);

                CombatActor player = turnFactory.CreateActor(
                    new ActorModel("turn.player", "Turn Player", ActorTeam.Player, new StatBlock(100f, 10f, 0f, 1f, 1f, 0f, 0f, 1f)),
                    Vector3.zero,
                    Color.white,
                    turnContext);
                CombatActor monster = turnFactory.CreateActor(
                    new ActorModel("turn.monster", "Turn Monster", ActorTeam.Monster, new StatBlock(100f, 5f, 0f, 1f, 1f, 0f, 0f, 1f)),
                    Vector3.right,
                    Color.white,
                    turnContext);

                Require(!player.GetComponent<AutoCombatController>().IsRuntimeActive, "Realtime actor loop should be inactive in TurnBased mode.");
                Require(!monster.GetComponent<AutoCombatController>().IsRuntimeActive, "Realtime monster loop should be inactive in TurnBased mode.");

                TurnBasedAutoBattleController turnLoop = loopRoot.AddComponent<TurnBasedAutoBattleController>();
                turnLoop.Initialize(turnContext, designerSettings.TurnCombat, true);
                Require(turnLoop.IsRuntimeActive, "Turn-based loop should be active when TurnBased mode is selected.");
                float monsterHpBefore = monster.Model.CurrentHp;
                Require(turnLoop.TryExecuteTurn(1f), "Turn-based loop did not execute when selected.");
                Require(monster.Model.CurrentHp < monsterHpBefore, "Turn-based loop did not damage the selected target.");

                turnLoop.SetRuntimeActive(false);
                Require(!turnLoop.TryExecuteTurn(1f), "Turn-based loop executed while inactive.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(loopRoot);
            }
        }

        private static void RequireCombatPolicies(GameObject _RuntimeRoot, BattleContext _Context, StageController _Stage)
        {
            Require(CombatRangePolicy.IsInsideAttackRange(_Stage.Player, _Stage.ActiveMonster, 999f), "Combat range policy did not apply padding.");
            Vector3 approachPosition = CombatRangePolicy.GetApproachPosition(
                _Stage.Player.transform.position,
                _Stage.ActiveMonster.transform.position,
                _Stage.Player.Model.Stats.AttackRange,
                _Context.Targeting.AttackRangePadding);
            Require(Vector3.Distance(approachPosition, _Stage.ActiveMonster.transform.position) > 0.01f, "Combat range policy produced an invalid approach position.");

            TurnBasedAutoBattleController turnBattle = _RuntimeRoot.AddComponent<TurnBasedAutoBattleController>();
            turnBattle.Initialize(_Context, new MvpTurnCombatSettings
            {
                TurnDelaySeconds = 0.01f,
                PlayerActsFirst = true
            }, true);

            float monsterHpBefore = _Stage.ActiveMonster.Model.CurrentHp;
            Vector3 playerPositionBefore = _Stage.Player.transform.position;
            Require(turnBattle.TryExecuteTurn(1f), "Turn-based auto battle did not execute a turn.");
            Require(Mathf.Approximately(_Stage.ActiveMonster.Model.CurrentHp, monsterHpBefore), "Turn-based auto battle damaged a target outside attack range.");
            Require(Vector3.Distance(_Stage.Player.transform.position, playerPositionBefore) > 0.01f, "Turn-based auto battle did not move toward a target outside attack range.");
        }

        private static void RequireSceneFlow(GameObject _RuntimeRoot)
        {
            StageSceneFlowController.ClearPendingBattleStage();
            StageSceneFlowController sceneFlow = _RuntimeRoot.AddComponent<StageSceneFlowController>();
            sceneFlow.Initialize(new MvpSceneFlowSettings
            {
                InitialMode = StageFlowMode.Field,
                LoadConfiguredScenes = false
            });
            Require(sceneFlow.CurrentMode == StageFlowMode.Field, "Scene flow did not initialize in field mode.");

            Transform fieldPlayer = CreateSmokeChild(_RuntimeRoot.transform, "Smoke Field Player").transform;
            Transform encounterPoint = CreateSmokeChild(_RuntimeRoot.transform, "Smoke Encounter Point").transform;
            fieldPlayer.position = Vector3.zero;
            encounterPoint.position = Vector3.zero;

            FieldEncounterController encounter = _RuntimeRoot.AddComponent<FieldEncounterController>();
            encounter.Initialize(
                new MvpFieldEncounterSettings
                {
                    Enabled = true,
                    TriggerMode = EncounterTriggerMode.Distance,
                    TriggerDistance = 0.75f,
                    BattleStageNumber = 4,
                    TriggerOnce = true
                },
                fieldPlayer,
                encounterPoint,
                sceneFlow);

            Require(encounter.TriggerEncounter(), "Field encounter did not trigger battle.");
            Require(sceneFlow.CurrentMode == StageFlowMode.Battle, "Field encounter did not switch to battle mode.");
            Require(sceneFlow.RequestedStageNumber == 4, "Field encounter did not request the configured battle stage.");
            Require(sceneFlow.HasBattleStageRequest, "Scene flow did not keep the pending battle request until it is acknowledged.");
            Require(!encounter.TriggerEncounter(), "Field encounter ignored Trigger Once.");

            sceneFlow.ClearBattleStageRequest();
            Require(!sceneFlow.HasBattleStageRequest, "Scene flow did not clear the acknowledged battle request.");

            GameObject fieldRoot = CreateSmokeChild(_RuntimeRoot.transform, "Field Runtime Smoke Root");
            BattleContext boundaryContext = fieldRoot.AddComponent<BattleContext>();
            StageController stage = fieldRoot.AddComponent<StageController>();
            MvpSceneDesignerSettings designerSettings = MvpSceneDesignerSettings.CreateDefault();
            ActorFactory factory = new ActorFactory(GeneratedSpriteFactory.CreateUnitSprite(), designerSettings.Actors, CombatLoopMode.Realtime);
            Transform spawnPoint = CreateSmokeChild(fieldRoot.transform, "Boundary Spawn Point").transform;
            stage.Initialize(new StageController.RuntimeSetup
            {
                Database = DemoContentFactory.CreateWeek1Database(),
                Context = boundaryContext,
                Factory = factory,
                MonsterSpawnPoint = spawnPoint,
                RuntimeSettings = designerSettings.Stage,
                ActorSettings = designerSettings.Actors,
                SpawnSettings = designerSettings.Spawn
            });
            Require(stage.HasActiveStage, "Boundary smoke stage did not create a battle stage.");

            TurnBasedAutoBattleController turnLoop = fieldRoot.AddComponent<TurnBasedAutoBattleController>();
            turnLoop.Initialize(boundaryContext, designerSettings.TurnCombat, true);
            Require(turnLoop.IsRuntimeActive, "Boundary turn loop did not start for battle mode.");
            stage.SetRealtimeCombatActive(false);
            turnLoop.SetRuntimeActive(false);
            stage.ClearRuntime();
            Require(!stage.HasActiveStage, "StageController should not keep an active battle stage in field mode.");
            Require(boundaryContext.Actors.Count == 0, "BattleContext should not keep battle actors in field mode.");
            Require(!turnLoop.IsRuntimeActive, "Turn-based combat should not remain active in field mode.");
            StageSceneFlowController.ClearPendingBattleStage();
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

        private static void RequireNoSceneCombatActors(UnityEngine.SceneManagement.Scene _Scene)
        {
            CombatActor[] actors = UnityEngine.Object.FindObjectsOfType<CombatActor>(true);
            foreach (CombatActor actor in actors)
            {
                if (actor != null && actor.gameObject.scene == _Scene)
                    throw new InvalidOperationException("Runtime actor should not be saved in the editor scene: " + actor.gameObject.name);
            }
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
                throw new InvalidOperationException(_Message);
        }
    }
}
#endif
