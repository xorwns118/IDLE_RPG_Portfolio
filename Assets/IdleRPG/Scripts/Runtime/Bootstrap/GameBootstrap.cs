using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Combat;
using IdleRPG.Runtime.Configuration;
using IdleRPG.Runtime.Stages;
using IdleRPG.Runtime.UI;
using UnityEngine;

namespace IdleRPG.Runtime.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        private static bool Bootstrapped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateAfterSceneLoad()
        {
            if (Bootstrapped
                || UnityEngine.Object.FindObjectOfType<MvpSceneController>() != null
                || UnityEngine.Object.FindObjectOfType<GameBootstrap>() != null)
            {
                return;
            }

            GameObject bootstrapObject = new GameObject("[IdleRPG] Game Bootstrap");
            bootstrapObject.AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            if (Bootstrapped)
            {
                Destroy(gameObject);
                return;
            }

            Bootstrapped = true;
            DontDestroyOnLoad(gameObject);
            InstallWeek1Demo();
        }

        private void InstallWeek1Demo()
        {
            Application.targetFrameRate = 60;
            MvpGameContentSettings contentSettings = MvpGameContentSettings.CreateDefault();
            MvpSceneDesignerSettings designerSettings = MvpSceneDesignerSettings.CreateDefault();

            Camera camera = EnsureCamera(designerSettings.Camera);
            Sprite sprite = GeneratedSpriteFactory.CreateUnitSprite();

            BattleContext context = gameObject.AddComponent<BattleContext>();
            ActorFactory actorFactory = new ActorFactory(sprite, designerSettings.Actors);

            StageController stageController = gameObject.AddComponent<StageController>();
            stageController.Initialize(new StageController.RuntimeSetup
            {
                Database = contentSettings.CreateDatabase(),
                Context = context,
                Factory = actorFactory,
                RuntimeSettings = designerSettings.Stage,
                ContentSettings = contentSettings,
                ActorSettings = designerSettings.Actors,
                PlayerStartPosition = designerSettings.World.PlayerStartPosition,
                SpawnSettings = designerSettings.Spawn
            });

            CombatHud hud = gameObject.AddComponent<CombatHud>();
            hud.Initialize(stageController, context);

            CreateGround(sprite, designerSettings.World);
            camera.transform.position = designerSettings.Camera.Position;
        }

        private static Camera EnsureCamera(MvpCameraSettings _Settings)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.orthographic = true;
            camera.orthographicSize = _Settings.OrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = _Settings.BackgroundColor;
            return camera;
        }

        private static void CreateGround(Sprite _Sprite, MvpWorldLayoutSettings _Settings)
        {
            GameObject ground = new GameObject("Combat Ground");
            ground.transform.position = _Settings.GroundPosition;
            ground.transform.localScale = _Settings.GroundScale;

            SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
            renderer.sprite = _Sprite;
            renderer.color = _Settings.GroundColor;
            renderer.sortingOrder = _Settings.GroundSortingOrder;
        }
    }
}
