using IdleRPG.Domain;
using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Combat;
using IdleRPG.Runtime.Configuration;
using IdleRPG.Runtime.UI;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace IdleRPG.Editor
{
    public sealed class ActorPrefabBuilderWindow : EditorWindow
    {
        [SerializeField] private ActorPrefabBuildSettings Settings;
        private Vector2 ScrollPosition;

        [MenuItem("Idle RPG/Prefabs/Actor/Create Actor Prefab", priority = 210)]
        public static void Open()
        {
            ActorPrefabBuilderWindow window = GetWindow<ActorPrefabBuilderWindow>("Actor Prefab");
            window.minSize = new Vector2(390f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            Settings ??= ActorPrefabBuildSettings.CreateDefaultActor();
            Settings.EnsureDefaults(ActorTeam.Player);
            Settings.AssignDefaultAnimatorAssets();
        }

        private void OnGUI()
        {
            Settings.EnsureDefaults(ActorTeam.Player);

            ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);
            ActorPrefabBuilderGui.DrawHeader(
                "Actor Prefab Builder",
                "Creates a controllable actor prefab with runtime components, label, HP bar, anchors, and editable stats.");

            ActorPrefabBuilderGui.DrawCommonSettings(Settings);
            ActorPrefabBuilderGui.DrawIdentitySettings(Settings);
            ActorPrefabBuilderGui.DrawStatSettings(Settings.Stats);
            ActorPrefabBuilderGui.DrawViewSettings(Settings);
            ActorPrefabBuilderGui.SetupPrefabAnimator(Settings);
            ActorPrefabBuilderGui.DrawComponentSettings(Settings);

            EditorGUILayout.Space(14f);
            if (GUILayout.Button("Create Actor Prefab", GUILayout.Height(34f)))
            {
                GameObject prefab = ActorPrefabBuilder.CreateActorPrefab(Settings);
                ActorPrefabBuilderGui.SelectPrefab(prefab);
                EditorUtility.DisplayDialog("Idle RPG", "Actor prefab was created at " + AssetDatabase.GetAssetPath(prefab) + ".", "OK");
            }

            EditorGUILayout.EndScrollView();
        }
    }

    public sealed class MonsterPrefabBuilderWindow : EditorWindow
    {
        [SerializeField] private ActorPrefabBuildSettings Settings;
        private Vector2 ScrollPosition;

        [MenuItem("Idle RPG/Prefabs/Monster/Create Monster Prefab", priority = 220)]
        public static void Open()
        {
            MonsterPrefabBuilderWindow window = GetWindow<MonsterPrefabBuilderWindow>("Monster Prefab");
            window.minSize = new Vector2(390f, 610f);
            window.Show();
        }

        private void OnEnable()
        {
            if (Settings == null)
                Settings = ActorPrefabBuildSettings.CreateDefaultMonster();

            Settings.EnsureDefaults(ActorTeam.Monster);
            Settings.AssignDefaultAnimatorAssets();
        }

        private void OnGUI()
        {
            Settings.EnsureDefaults(ActorTeam.Monster);

            ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);
            ActorPrefabBuilderGui.DrawHeader(
                "Monster Prefab Builder",
                "Creates a monster prefab with runtime components, label, HP bar, anchors, editable stats, and reward values.");

            ActorPrefabBuilderGui.DrawCommonSettings(Settings);
            ActorPrefabBuilderGui.DrawIdentitySettings(Settings);
            ActorPrefabBuilderGui.DrawStatSettings(Settings.Stats);
            ActorPrefabBuilderGui.DrawRewardSettings(Settings);
            ActorPrefabBuilderGui.DrawViewSettings(Settings);
            ActorPrefabBuilderGui.SetupPrefabAnimator(Settings);
            ActorPrefabBuilderGui.DrawComponentSettings(Settings);

            EditorGUILayout.Space(14f);
            if (GUILayout.Button("Create Monster Prefab", GUILayout.Height(34f)))
            {
                GameObject prefab = ActorPrefabBuilder.CreateMonsterPrefab(Settings);
                ActorPrefabBuilderGui.SelectPrefab(prefab);
                EditorUtility.DisplayDialog("Idle RPG", "Monster prefab was created at " + AssetDatabase.GetAssetPath(prefab) + ".", "OK");
            }

            EditorGUILayout.EndScrollView();
        }
    }

    public static class ActorPrefabBuilder
    {
        [MenuItem("Idle RPG/Prefabs/Actor/Create Default Actor Prefab", priority = 211)]
        public static void CreateDefaultActorPrefab()
        {
            GameObject prefab = CreateActorPrefab(ActorPrefabBuildSettings.CreateDefaultActorWithAnimationAssets());
            ShowCreatedDialog(prefab, "Default actor prefab was created.");
        }

        [MenuItem("Idle RPG/Prefabs/Monster/Create Default Monster Prefab", priority = 221)]
        public static void CreateDefaultMonsterPrefab()
        {
            GameObject prefab = CreateMonsterPrefab(ActorPrefabBuildSettings.CreateDefaultMonsterWithAnimationAssets());
            ShowCreatedDialog(prefab, "Default monster prefab was created.");
        }

        public static void CreateDefaultActorAndMonsterPrefabs()
        {
            CreateActorPrefab(ActorPrefabBuildSettings.CreateDefaultActorWithAnimationAssets());
            CreateMonsterPrefab(ActorPrefabBuildSettings.CreateDefaultMonsterWithAnimationAssets());
        }

        public static GameObject CreateActorPrefab(ActorPrefabBuildSettings _Settings)
        {
            ActorPrefabBuildSettings settings = _Settings ?? ActorPrefabBuildSettings.CreateDefaultActor();
            settings.EnsureDefaults(ActorTeam.Player);
            settings.Team = ActorTeam.Player;
            settings.GoldReward = 0;
            settings.ExpReward = 0;
            return CreatePrefab(settings);
        }

        public static GameObject CreateMonsterPrefab(ActorPrefabBuildSettings _Settings)
        {
            ActorPrefabBuildSettings settings = _Settings ?? ActorPrefabBuildSettings.CreateDefaultMonster();
            settings.EnsureDefaults(ActorTeam.Monster);
            settings.Team = ActorTeam.Monster;
            return CreatePrefab(settings);
        }

        private static GameObject CreatePrefab(ActorPrefabBuildSettings _Settings)
        {
            EnsureAssetFolder(_Settings.OutputFolder);

            GameObject root = new GameObject(_Settings.PrefabName);
            try
            {
                string assetPath = BuildPrefabPath(_Settings.OutputFolder, _Settings.PrefabName, _Settings.OverwriteExisting);
                ConfigureRoot(root, _Settings, assetPath);
                CreateNameLabel(root.transform, _Settings.DisplayName, _Settings.SortingOrder + _Settings.LabelSortingOrderOffset);
                CreateHpBarChildren(root.transform, _Settings);

                if (_Settings.IncludeEffectAnchors)
                    CreateEffectAnchors(root.transform);

                GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                if (prefabAsset == null)
                    throw new InvalidOperationException("Failed to save actor prefab at " + assetPath + ".");

                return prefabAsset;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureRoot(GameObject _Root, ActorPrefabBuildSettings _Settings, string _PrefabAssetPath)
        {
            _Root.transform.localScale = _Settings.PreviewScale;

            SpriteRenderer spriteRenderer = _Root.AddComponent<SpriteRenderer>();
            spriteRenderer.color = _Settings.WorldColor;
            spriteRenderer.sortingOrder = _Settings.SortingOrder;

            ActorPrefabProfile profile = _Root.AddComponent<ActorPrefabProfile>();
            profile.Configure(
                _Settings.Team,
                _Settings.Id,
                _Settings.DisplayName,
                _Settings.WorldColor,
                _Settings.Stats,
                _Settings.GoldReward,
                _Settings.ExpReward);

            _Root.AddComponent<CombatActor>();
            _Root.AddComponent<HealthBarView>();
            _Root.AddComponent<AutoCombatController>();

            if (_Settings.IncludeAnimator)
            {
                Animator animator = _Root.AddComponent<Animator>();
                AnimatorOverrideController overrideController = CreateAnimatorOverrideAsset(_Settings, _PrefabAssetPath);
                if (overrideController != null)
                    animator.runtimeAnimatorController = overrideController;

                ActorAnimationView animationView = _Root.AddComponent<ActorAnimationView>();
                animationView.Configure(overrideController, _Settings.Animation);
            }

            if (_Settings.IncludeCollider)
            {
                BoxCollider2D collider = _Root.AddComponent<BoxCollider2D>();
                collider.size = _Settings.ColliderSize;
                collider.offset = _Settings.ColliderOffset;
            }
        }

        private static void CreateNameLabel(Transform _Root, string _PreviewDisplayName, int _SortingOrder)
        {
            GameObject label = new GameObject("Name Label");
            label.transform.SetParent(_Root, false);
            label.transform.localPosition = new Vector3(0f, 1.35f, 0f);

            TextMesh textMesh = label.AddComponent<TextMesh>();
            textMesh.text = _PreviewDisplayName;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.18f;
            textMesh.fontSize = 36;
            textMesh.color = Color.white;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                textMesh.font = font;

            MeshRenderer renderer = label.GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = label.AddComponent<MeshRenderer>();

            renderer.sortingOrder = _SortingOrder;
            if (textMesh.font != null)
                renderer.sharedMaterial = textMesh.font.material;
        }

        private static void CreateHpBarChildren(Transform _Root, ActorPrefabBuildSettings _Settings)
        {
            MvpHealthBarSettings healthBar = new MvpHealthBarSettings();
            CreateHpBarChild(
                _Root,
                "HP Background",
                healthBar.Offset,
                new Vector3(healthBar.Width, healthBar.Height, 1f),
                healthBar.BackgroundColor,
                healthBar.BackgroundSortingOrder);
            CreateHpBarChild(
                _Root,
                "HP Fill",
                healthBar.Offset + new Vector3(0f, 0f, healthBar.FillDepthOffset),
                new Vector3(healthBar.Width, healthBar.Height, 1f),
                _Settings.WorldColor,
                healthBar.FillSortingOrder);
        }

        private static void CreateHpBarChild(Transform _Root, string _Name, Vector3 _Position, Vector3 _Scale, Color _Color, int _SortingOrder)
        {
            GameObject bar = new GameObject(_Name);
            bar.transform.SetParent(_Root, false);
            bar.transform.localPosition = _Position;
            bar.transform.localScale = _Scale;

            SpriteRenderer renderer = bar.AddComponent<SpriteRenderer>();
            renderer.color = _Color;
            renderer.sortingOrder = _SortingOrder;
        }

        private static void CreateEffectAnchors(Transform _Root)
        {
            GameObject anchors = new GameObject("Effect Anchors");
            anchors.transform.SetParent(_Root, false);

            CreateAnchor(anchors.transform, "Hit Point", new Vector3(0f, 0.45f, 0f));
            CreateAnchor(anchors.transform, "Projectile Point", new Vector3(0.35f, 0.45f, 0f));
            CreateAnchor(anchors.transform, "Ground Point", new Vector3(0f, -0.55f, 0f));
        }

        private static void CreateAnchor(Transform _Parent, string _Name, Vector3 _LocalPosition)
        {
            GameObject anchor = new GameObject(_Name);
            anchor.transform.SetParent(_Parent, false);
            anchor.transform.localPosition = _LocalPosition;
        }

        private static string BuildPrefabPath(string _OutputFolder, string _PrefabName, bool _OverwriteExisting)
        {
            string path = _OutputFolder.TrimEnd('/') + "/" + MakeSafeAssetName(_PrefabName) + ".prefab";
            return _OverwriteExisting ? path : AssetDatabase.GenerateUniqueAssetPath(path);
        }

        private static AnimatorOverrideController CreateAnimatorOverrideAsset(ActorPrefabBuildSettings _Settings, string _PrefabAssetPath)
        {
            if (_Settings.PrefAnimatorController == null)
                return null;

            RuntimeAnimatorController baseController = _Settings.PrefAnimatorController.runtimeAnimatorController;
            if (baseController == null)
                throw new InvalidOperationException("Animator Override Controller needs a base Animator Controller.");

            string overridePath = BuildAnimatorOverridePath(_PrefabAssetPath, _Settings.OverwriteExisting);
            AnimatorOverrideController overrideController = _Settings.OverwriteExisting
                ? AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath)
                : null;

            if (overrideController == null)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(overridePath) != null)
                    overridePath = AssetDatabase.GenerateUniqueAssetPath(overridePath);

                overrideController = new AnimatorOverrideController(baseController);
                AssetDatabase.CreateAsset(overrideController, overridePath);
            }
            else
            {
                overrideController.runtimeAnimatorController = baseController;
            }

            CopyAnimatorOverrides(_Settings.PrefAnimatorController, overrideController);
            ApplyAnimatorClipOverrides(overrideController, _Settings);
            EditorUtility.SetDirty(overrideController);
            AssetDatabase.SaveAssets();
            return overrideController;
        }

        private static string BuildAnimatorOverridePath(string _PrefabAssetPath, bool _OverwriteExisting)
        {
            string folder = System.IO.Path.GetDirectoryName(_PrefabAssetPath)?.Replace('\\', '/');
            string fileName = System.IO.Path.GetFileNameWithoutExtension(_PrefabAssetPath);
            string path = folder + "/" + fileName + "_Animator.overrideController";
            return _OverwriteExisting ? path : AssetDatabase.GenerateUniqueAssetPath(path);
        }

        private static void CopyAnimatorOverrides(AnimatorOverrideController _Source, AnimatorOverrideController _Target)
        {
            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            _Source.GetOverrides(overrides);
            if (overrides.Count > 0)
                _Target.ApplyOverrides(overrides);
        }

        private static void ApplyAnimatorClipOverrides(AnimatorOverrideController _Controller, ActorPrefabBuildSettings _Settings)
        {
            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            _Controller.GetOverrides(overrides);

            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip sourceClip = overrides[i].Key;
                AnimationClip replacementClip = GetConfiguredOverrideClip(sourceClip, _Settings);
                if (replacementClip != null)
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(sourceClip, replacementClip);
            }

            _Controller.ApplyOverrides(overrides);
        }

        private static AnimationClip GetConfiguredOverrideClip(AnimationClip _SourceClip, ActorPrefabBuildSettings _Settings)
        {
            if (_SourceClip == null)
                return null;

            string normalizedName = _SourceClip.name.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
            if (normalizedName.Contains("idle") && normalizedName.Contains("left"))
                return _Settings.Idle_Left;

            if (normalizedName.Contains("idle") && normalizedName.Contains("right"))
                return _Settings.Idle_Right;

            if (normalizedName.Contains("walk") && normalizedName.Contains("left"))
                return _Settings.Walk_Left;

            if (normalizedName.Contains("walk") && normalizedName.Contains("right"))
                return _Settings.Walk_Right;

            return null;
        }

        private static string MakeSafeAssetName(string _Name)
        {
            string name = string.IsNullOrWhiteSpace(_Name) ? "Actor_Base" : _Name.Trim();
            foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalidChar, '_');
            }

            return name;
        }

        private static void EnsureAssetFolder(string _Folder)
        {
            if (string.IsNullOrWhiteSpace(_Folder) || !_Folder.StartsWith("Assets", StringComparison.Ordinal))
                throw new ArgumentException("Prefab output folder must be inside the Assets folder.");

            string normalizedFolder = _Folder.Replace('\\', '/').TrimEnd('/');
            string[] parts = normalizedFolder.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                    AssetDatabase.CreateFolder(currentPath, parts[i]);

                currentPath = nextPath;
            }
        }

        private static void ShowCreatedDialog(GameObject _Prefab, string _Message)
        {
            if (Application.isBatchMode)
                return;

            ActorPrefabBuilderGui.SelectPrefab(_Prefab);
            EditorUtility.DisplayDialog("Idle RPG", _Message + "\n\n" + AssetDatabase.GetAssetPath(_Prefab), "OK");
        }
    }

    internal static class ActorPrefabBuilderGui
    {
        public static void DrawHeader(string _Title, string _Description)
        {
            EditorGUILayout.LabelField(_Title, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_Description, MessageType.Info);
            EditorGUILayout.Space(6f);
        }

        public static void DrawCommonSettings(ActorPrefabBuildSettings _Settings)
        {
            EditorGUILayout.LabelField("Asset", EditorStyles.boldLabel);
            _Settings.OutputFolder = EditorGUILayout.TextField(
                new GUIContent("Output Folder", "Asset folder where this prefab will be saved."),
                _Settings.OutputFolder);
            _Settings.PrefabName = EditorGUILayout.TextField(
                new GUIContent("Prefab Name", "Saved prefab asset name."),
                _Settings.PrefabName);
            _Settings.OverwriteExisting = EditorGUILayout.Toggle(
                new GUIContent("Overwrite Existing", "When disabled, Unity generates a unique path instead of replacing an existing prefab."),
                _Settings.OverwriteExisting);
            EditorGUILayout.Space(8f);
        }

        public static void DrawIdentitySettings(ActorPrefabBuildSettings _Settings)
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Team", _Settings.Team);
            }

            _Settings.Id = EditorGUILayout.TextField(
                new GUIContent("Id", "Stable id used by content tables, save data, and stage references."),
                _Settings.Id);
            _Settings.DisplayName = EditorGUILayout.TextField(
                new GUIContent("Display Name", "Name shown in world labels and HUD."),
                _Settings.DisplayName);
            EditorGUILayout.Space(8f);
        }

        public static void DrawStatSettings(MvpStatBlockSettings _Stats)
        {
            EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
            _Stats.MaxHp = EditorGUILayout.FloatField(new GUIContent("Max HP"), _Stats.MaxHp);
            _Stats.AttackPower = EditorGUILayout.FloatField(new GUIContent("Attack Power"), _Stats.AttackPower);
            _Stats.Defense = EditorGUILayout.FloatField(new GUIContent("Defense"), _Stats.Defense);
            _Stats.AttackRange = EditorGUILayout.FloatField(new GUIContent("Attack Range"), _Stats.AttackRange);
            _Stats.AttackInterval = EditorGUILayout.FloatField(new GUIContent("Attack Interval"), _Stats.AttackInterval);
            _Stats.MoveSpeed = EditorGUILayout.FloatField(new GUIContent("Move Speed"), _Stats.MoveSpeed);
            _Stats.CriticalChance = EditorGUILayout.Slider(new GUIContent("Critical Chance"), _Stats.CriticalChance, 0f, 1f);
            _Stats.CriticalMultiplier = EditorGUILayout.FloatField(new GUIContent("Critical Multiplier"), _Stats.CriticalMultiplier);
            NormalizeStats(_Stats);
            EditorGUILayout.Space(8f);
        }

        public static void DrawRewardSettings(ActorPrefabBuildSettings _Settings)
        {
            EditorGUILayout.LabelField("Reward", EditorStyles.boldLabel);
            _Settings.GoldReward = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Gold Reward"), _Settings.GoldReward));
            _Settings.ExpReward = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("EXP Reward"), _Settings.ExpReward));
            EditorGUILayout.Space(8f);
        }

        public static void DrawViewSettings(ActorPrefabBuildSettings _Settings)
        {
            EditorGUILayout.LabelField("Preview View", EditorStyles.boldLabel);
            _Settings.WorldColor = EditorGUILayout.ColorField(new GUIContent("World Color"), _Settings.WorldColor);
            _Settings.PreviewScale = EditorGUILayout.Vector3Field(new GUIContent("Preview Scale"), _Settings.PreviewScale);
            _Settings.SortingOrder = EditorGUILayout.IntField(new GUIContent("Sorting Order"), _Settings.SortingOrder);
            _Settings.LabelSortingOrderOffset = EditorGUILayout.IntField(
                new GUIContent("Label Sorting Offset"),
                _Settings.LabelSortingOrderOffset);
            EditorGUILayout.Space(8f);
        }

        public static void SetupPrefabAnimator(ActorPrefabBuildSettings _Settings)
        {
            EditorGUILayout.LabelField("Animator", EditorStyles.boldLabel);
            _Settings.IncludeAnimator = EditorGUILayout.Toggle(
                new GUIContent("Include Animator", "Adds an Animator and assigns a generated Animator Override Controller when a template is provided."),
                _Settings.IncludeAnimator);

            using (new EditorGUI.DisabledScope(!_Settings.IncludeAnimator))
            {
                if (GUILayout.Button("Use Link Animation Defaults"))
                    _Settings.AssignDefaultAnimatorAssets();

                _Settings.PrefAnimatorController = (AnimatorOverrideController)EditorGUILayout.ObjectField(
                    new GUIContent("Override Controller Template", "Template Animator Override Controller. A prefab-specific copy is generated on prefab creation."),
                    _Settings.PrefAnimatorController,
                    typeof(AnimatorOverrideController),
                    false);

                _Settings.Idle_Left = (AnimationClip)EditorGUILayout.ObjectField("Idle Left", _Settings.Idle_Left, typeof(AnimationClip), false);
                _Settings.Idle_Right = (AnimationClip)EditorGUILayout.ObjectField("Idle Right", _Settings.Idle_Right, typeof(AnimationClip), false);
                _Settings.Walk_Left = (AnimationClip)EditorGUILayout.ObjectField("Walk Left", _Settings.Walk_Left, typeof(AnimationClip), false);
                _Settings.Walk_Right = (AnimationClip)EditorGUILayout.ObjectField("Walk Right", _Settings.Walk_Right, typeof(AnimationClip), false);

                _Settings.Animation.Enabled = EditorGUILayout.Toggle(
                    new GUIContent("Drive Animator Parameters", "Updates IsWalk and IsLeft while the actor moves at runtime."),
                    _Settings.Animation.Enabled);
                _Settings.Animation.WalkParameterName = EditorGUILayout.TextField(
                    new GUIContent("Walk Parameter", "Bool parameter used for walk/idle state transitions."),
                    _Settings.Animation.WalkParameterName);
                _Settings.Animation.LeftParameterName = EditorGUILayout.TextField(
                    new GUIContent("Left Parameter", "Bool parameter used for facing direction transitions."),
                    _Settings.Animation.LeftParameterName);
                _Settings.Animation.MovementThreshold = Mathf.Max(
                    0f,
                    EditorGUILayout.FloatField(
                        new GUIContent("Movement Threshold", "Minimum frame movement treated as walking."),
                        _Settings.Animation.MovementThreshold));
                _Settings.Animation.MirrorSpriteRendererByFacing = EditorGUILayout.Toggle(
                    new GUIContent("Mirror Sprite By Facing", "Keeps SpriteRenderer.flipX synced with facing direction for mirrored sprite sets."),
                    _Settings.Animation.MirrorSpriteRendererByFacing);

                if (_Settings.PrefAnimatorController == null)
                    EditorGUILayout.HelpBox("Assign an Animator Override Controller template to generate and connect prefab-specific animation overrides.", MessageType.Info);
            }

            EditorGUILayout.Space(8f);
        }


        public static void DrawComponentSettings(ActorPrefabBuildSettings _Settings)
        {
            EditorGUILayout.LabelField("Components", EditorStyles.boldLabel);
            _Settings.IncludeCollider = EditorGUILayout.Toggle(
                new GUIContent("Include Collider", "Adds a BoxCollider2D sized for a simple vertical actor body."),
                _Settings.IncludeCollider);

            using (new EditorGUI.DisabledScope(!_Settings.IncludeCollider))
            {
                _Settings.ColliderSize = EditorGUILayout.Vector2Field(new GUIContent("Collider Size"), _Settings.ColliderSize);
                _Settings.ColliderOffset = EditorGUILayout.Vector2Field(new GUIContent("Collider Offset"), _Settings.ColliderOffset);
            }

            _Settings.IncludeEffectAnchors = EditorGUILayout.Toggle(
                new GUIContent("Include Effect Anchors", "Adds named child transforms for hit, projectile, and ground effect attachment points."),
                _Settings.IncludeEffectAnchors);
        }

        public static void SelectPrefab(GameObject _Prefab)
        {
            if (_Prefab == null)
                return;

            Selection.activeObject = _Prefab;
            EditorGUIUtility.PingObject(_Prefab);
        }

        private static void NormalizeStats(MvpStatBlockSettings _Stats)
        {
            _Stats.MaxHp = Mathf.Max(1f, _Stats.MaxHp);
            _Stats.AttackPower = Mathf.Max(0f, _Stats.AttackPower);
            _Stats.Defense = Mathf.Max(0f, _Stats.Defense);
            _Stats.AttackRange = Mathf.Max(0.1f, _Stats.AttackRange);
            _Stats.AttackInterval = Mathf.Max(0.1f, _Stats.AttackInterval);
            _Stats.MoveSpeed = Mathf.Max(0f, _Stats.MoveSpeed);
            _Stats.CriticalChance = Mathf.Clamp01(_Stats.CriticalChance);
            _Stats.CriticalMultiplier = Mathf.Max(1f, _Stats.CriticalMultiplier);
        }
    }

    [Serializable]
    public sealed class ActorPrefabBuildSettings
    {
        public ActorTeam Team = ActorTeam.Player;
        public string OutputFolder = "Assets/IdleRPG/Prefabs/Actors";
        public bool OverwriteExisting = true;
        public string PrefabName = "Hero_Base";
        public string Id = "player.hero";
        public string DisplayName = "Training Hero";
        public Color WorldColor = new Color(0.25f, 0.7f, 1f);
        public MvpStatBlockSettings Stats = MvpStatBlockSettings.Create(140f, 16f, 3f, 1.05f, 0.7f, 2.35f, 0.15f, 1.5f);
        public int GoldReward;
        public int ExpReward;
        public Vector3 PreviewScale = new Vector3(0.85f, 1.25f, 1f);
        public int SortingOrder = 10;
        public int LabelSortingOrderOffset = 20;
        public bool IncludeAnimator = true;
        public bool IncludeCollider = true;
        public Vector2 ColliderSize = new Vector2(0.75f, 1.15f);
        public Vector2 ColliderOffset = new Vector2(0f, 0.08f);
        public bool IncludeEffectAnchors = true;
        public AnimatorOverrideController PrefAnimatorController;
        public AnimationClip Idle_Right;
        public AnimationClip Idle_Left;
        public AnimationClip Walk_Right;
        public AnimationClip Walk_Left;
        public MvpActorAnimationSettings Animation = new MvpActorAnimationSettings();

        public static ActorPrefabBuildSettings CreateDefaultActor()
        {
            return new ActorPrefabBuildSettings();
        }

        public static ActorPrefabBuildSettings CreateDefaultActorWithAnimationAssets()
        {
            ActorPrefabBuildSettings settings = CreateDefaultActor();
            settings.AssignDefaultAnimatorAssets();
            return settings;
        }

        public static ActorPrefabBuildSettings CreateDefaultMonster()
        {
            return new ActorPrefabBuildSettings
            {
                Team = ActorTeam.Monster,
                OutputFolder = "Assets/IdleRPG/Prefabs/Monsters",
                PrefabName = "Monster_Base",
                Id = "monster.base",
                DisplayName = "Training Monster",
                WorldColor = new Color(0.35f, 0.9f, 0.55f),
                Stats = MvpStatBlockSettings.Create(32f, 5f, 0f, 0.85f, 1.25f, 1.2f, 0.02f, 1.25f),
                GoldReward = 5,
                ExpReward = 2,
                PreviewScale = new Vector3(0.8f, 1f, 1f),
                SortingOrder = 9
            };
        }

        public static ActorPrefabBuildSettings CreateDefaultMonsterWithAnimationAssets()
        {
            ActorPrefabBuildSettings settings = CreateDefaultMonster();
            settings.AssignDefaultAnimatorAssets();
            return settings;
        }

        public void AssignDefaultAnimatorAssets()
        {
            if (PrefAnimatorController == null)
                PrefAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>("Assets/IdleRPG/Animations/Default_Character.overrideController");

            if (Idle_Left == null)
                Idle_Left = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/IdleRPG/Animations/Link_Idle_Left.anim");

            if (Idle_Right == null)
                Idle_Right = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/IdleRPG/Animations/Link_Idle_Right.anim");

            if (Walk_Left == null)
                Walk_Left = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/IdleRPG/Animations/Link_Walk_Left.anim");

            if (Walk_Right == null)
                Walk_Right = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/IdleRPG/Animations/Link_Walk_Right.anim");
        }

        public void EnsureDefaults(ActorTeam _RequiredTeam)
        {
            Team = _RequiredTeam;
            OutputFolder = string.IsNullOrWhiteSpace(OutputFolder)
                ? (_RequiredTeam == ActorTeam.Player ? "Assets/IdleRPG/Prefabs/Actors" : "Assets/IdleRPG/Prefabs/Monsters")
                : OutputFolder.Replace('\\', '/').TrimEnd('/');

            if (string.IsNullOrWhiteSpace(PrefabName))
                PrefabName = _RequiredTeam == ActorTeam.Player ? "Hero_Base" : "Monster_Base";

            if (string.IsNullOrWhiteSpace(Id))
                Id = _RequiredTeam == ActorTeam.Player ? "player.hero" : "monster.base";

            if (string.IsNullOrWhiteSpace(DisplayName))
                DisplayName = Id;

            if (Stats == null)
            {
                Stats = _RequiredTeam == ActorTeam.Player
                    ? MvpStatBlockSettings.Create(140f, 16f, 3f, 1.05f, 0.7f, 2.35f, 0.15f, 1.5f)
                    : MvpStatBlockSettings.Create(32f, 5f, 0f, 0.85f, 1.25f, 1.2f, 0.02f, 1.25f);
            }

            if (Animation == null)
                Animation = new MvpActorAnimationSettings();

            Animation.EnsureDefaults();

            GoldReward = Mathf.Max(0, GoldReward);
            ExpReward = Mathf.Max(0, ExpReward);
        }
    }
}
