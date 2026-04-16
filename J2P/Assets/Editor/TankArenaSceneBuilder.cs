using System.IO;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TankArena2D.Editor
{
    public static class TankArenaSceneBuilder
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string MainScenePath = "Assets/Game_v0.1.unity";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string TrainingScenePath = "Assets/Scenes/TrainingArena.unity";
        private const string FloorSpritePath = "Assets/Scenes/Floor.png";
        private const string CircleSpritePath = "Assets/Scenes/Target.png";
        private const string SquareSpritePath = "Assets/Scenes/Wall.png";

        private static readonly ObstacleSpec[] LargeArenaLayout =
        {
            new ObstacleSpec(new Vector2(-28f, 18f), new Vector2(4f, 4f), 45f),
            new ObstacleSpec(new Vector2(-18f, 20f), new Vector2(7f, 2f), 0f),
            new ObstacleSpec(new Vector2(-6f, 18f), new Vector2(3.5f, 3.5f), 45f),
            new ObstacleSpec(new Vector2(8f, 20f), new Vector2(8f, 2f), 0f),
            new ObstacleSpec(new Vector2(24f, 17f), new Vector2(4f, 4f), 45f),
            new ObstacleSpec(new Vector2(-32f, 7f), new Vector2(2.2f, 9f), 0f),
            new ObstacleSpec(new Vector2(-20f, 8f), new Vector2(4f, 4f), 45f),
            new ObstacleSpec(new Vector2(-10f, 7f), new Vector2(10f, 2.2f), 0f),
            new ObstacleSpec(new Vector2(4f, 7f), new Vector2(4.2f, 4.2f), 45f),
            new ObstacleSpec(new Vector2(18f, 8f), new Vector2(10f, 2.2f), 0f),
            new ObstacleSpec(new Vector2(32f, 5f), new Vector2(2.2f, 10f), 0f),
            new ObstacleSpec(new Vector2(-24f, -1f), new Vector2(10f, 2f), 0f),
            new ObstacleSpec(new Vector2(-8f, -2f), new Vector2(4f, 4f), 45f),
            new ObstacleSpec(new Vector2(8f, 0f), new Vector2(12f, 2.2f), 0f),
            new ObstacleSpec(new Vector2(25f, -2f), new Vector2(4f, 4f), 45f),
            new ObstacleSpec(new Vector2(-33f, -14f), new Vector2(2.2f, 11f), 0f),
            new ObstacleSpec(new Vector2(-20f, -15f), new Vector2(8f, 2f), 0f),
            new ObstacleSpec(new Vector2(-6f, -16f), new Vector2(3.8f, 3.8f), 45f),
            new ObstacleSpec(new Vector2(8f, -14f), new Vector2(8f, 2f), 0f),
            new ObstacleSpec(new Vector2(21f, -16f), new Vector2(4f, 4f), 45f),
            new ObstacleSpec(new Vector2(34f, -12f), new Vector2(2.2f, 12f), 0f),
            new ObstacleSpec(new Vector2(-14f, 0f), new Vector2(2.2f, 8f), 0f),
            new ObstacleSpec(new Vector2(14f, 0f), new Vector2(2.2f, 8f), 0f),
            new ObstacleSpec(new Vector2(0f, 13f), new Vector2(2.2f, 8f), 0f),
            new ObstacleSpec(new Vector2(0f, -13f), new Vector2(2.2f, 8f), 0f),
            new ObstacleSpec(new Vector2(-1f, 0f), new Vector2(5f, 5f), 45f),
            new ObstacleSpec(new Vector2(-38f, 0f), new Vector2(4f, 4f), 45f),
            new ObstacleSpec(new Vector2(38f, 0f), new Vector2(4f, 4f), 45f)
        };

        private static readonly ObstacleSpec[] TrainingArenaLayout =
        {
            new ObstacleSpec(new Vector2(-10f, 8f), new Vector2(3f, 3f), 45f),
            new ObstacleSpec(new Vector2(0f, 10f), new Vector2(8f, 1.8f), 0f),
            new ObstacleSpec(new Vector2(10f, 8f), new Vector2(3f, 3f), 45f),
            new ObstacleSpec(new Vector2(-12f, 0f), new Vector2(1.8f, 8f), 0f),
            new ObstacleSpec(new Vector2(12f, 0f), new Vector2(1.8f, 8f), 0f),
            new ObstacleSpec(new Vector2(0f, 0f), new Vector2(4f, 4f), 45f),
            new ObstacleSpec(new Vector2(-10f, -8f), new Vector2(3f, 3f), 45f),
            new ObstacleSpec(new Vector2(0f, -10f), new Vector2(8f, 1.8f), 0f),
            new ObstacleSpec(new Vector2(10f, -8f), new Vector2(3f, 3f), 45f)
        };

        [MenuItem("Tools/Tank Arena/Rebuild Authored Scenes")]
        public static void BuildAllScenes()
        {
            BuildMainMenuScene(MainMenuScenePath);
            BuildGameplayScene(MainScenePath);
            BuildGameplayScene(SampleScenePath);
            BuildTrainingArenaScene(TrainingScenePath);
            ApplyBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Tank Arena/Rebuild Main Game Scene")]
        public static void BuildGameScene()
        {
            BuildGameplayScene(MainScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Tank Arena/Rebuild Training Scene")]
        public static void BuildTrainingScene()
        {
            BuildTrainingArenaScene(TrainingScenePath);
            ApplyBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Tank Arena/Rebuild Main Menu Scene")]
        public static void BuildMainMenu()
        {
            BuildMainMenuScene(MainMenuScenePath);
            ApplyBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildMainMenuScene(string scenePath)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateStaticCamera(new Vector3(0f, 0f, -10f), 8f);

            GameObject root = new GameObject("MainMenuRoot");
            root.AddComponent<MainMenuController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EnsureDirectory(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void BuildGameplayScene(string scenePath)
        {
            Sprite floorSprite = LoadSprite(FloorSpritePath);
            Sprite circleSprite = LoadSprite(CircleSpritePath);
            Sprite squareSprite = LoadSprite(SquareSpritePath);
            ValidateSprites(floorSprite, circleSprite, squareSprite);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = new GameObject("TankArena");
            GameObject environmentRoot = CreateChild(root.transform, "Environment");
            GameObject actorsRoot = CreateChild(root.transform, "Actors");
            GameObject systemsRoot = CreateChild(root.transform, "Systems");
            GameObject projectilesRoot = CreateChild(actorsRoot.transform, "Projectiles");
            GameObject enemiesRoot = CreateChild(actorsRoot.transform, "Enemies");
            GameObject templatesRoot = CreateChild(root.transform, "Templates");

            Vector2 arenaSize = new Vector2(104f, 72f);
            const float wallThickness = 1.6f;

            ArenaBounds arenaBounds = new GameObject("ArenaBounds").AddComponent<ArenaBounds>();
            arenaBounds.transform.SetParent(environmentRoot.transform, false);
            arenaBounds.Configure(arenaSize, 3f);

            CreateFloor(environmentRoot.transform, floorSprite, arenaSize);
            CreateBoundaryWalls(environmentRoot.transform, squareSprite, arenaSize, wallThickness);
            CreateObstacleField(environmentRoot.transform, squareSprite, LargeArenaLayout);

            Projectile projectileTemplate = CreateProjectileTemplate(templatesRoot.transform, circleSprite);
            PlayerController player = CreatePlayer(actorsRoot.transform, circleSprite, squareSprite, projectileTemplate, projectilesRoot.transform, arenaBounds);
            Camera camera = CreateFollowCamera(player.transform, arenaBounds, 12f);
            player.Configure(camera, true);

            GameObject rulesEnemyTemplate = CreateRulesEnemyTemplate(
                templatesRoot.transform,
                "EnemyTemplateRules",
                circleSprite,
                squareSprite,
                projectileTemplate,
                projectilesRoot.transform,
                arenaBounds,
                player.transform);

            GameObject mlEnemyTemplate = CreateMlEnemyTemplate(
                templatesRoot.transform,
                "EnemyTemplateML",
                circleSprite,
                squareSprite,
                projectileTemplate,
                projectilesRoot.transform,
                arenaBounds,
                player.transform,
                EnemyAgentExecutionMode.Auto,
                null,
                null);

            SpawnManager spawnManager = new GameObject("SpawnManager").AddComponent<SpawnManager>();
            spawnManager.transform.SetParent(systemsRoot.transform, false);
            spawnManager.Configure(arenaBounds, rulesEnemyTemplate, player.transform, 14f, 4f, 0.9f, 0.15f, enemiesRoot.transform);

            GameManager gameManager = new GameObject("GameManager").AddComponent<GameManager>();
            gameManager.transform.SetParent(systemsRoot.transform, false);
            gameManager.Configure(arenaBounds, spawnManager, player, 6, 6, 3f, 2.5f, 3, 100, 4f);

            GameHud hud = new GameObject("GameHud").AddComponent<GameHud>();
            hud.transform.SetParent(systemsRoot.transform, false);
            hud.Configure(gameManager);

            PauseMenuController pauseMenu = new GameObject("PauseMenu").AddComponent<PauseMenuController>();
            pauseMenu.transform.SetParent(systemsRoot.transform, false);

            GameOverOverlay gameOverOverlay = new GameObject("GameOverOverlay").AddComponent<GameOverOverlay>();
            gameOverOverlay.transform.SetParent(systemsRoot.transform, false);

            projectileTemplate.gameObject.SetActive(false);
            rulesEnemyTemplate.SetActive(false);
            mlEnemyTemplate.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EnsureDirectory(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void BuildTrainingArenaScene(string scenePath)
        {
            Sprite floorSprite = LoadSprite(FloorSpritePath);
            Sprite circleSprite = LoadSprite(CircleSpritePath);
            Sprite squareSprite = LoadSprite(SquareSpritePath);
            ValidateSprites(floorSprite, circleSprite, squareSprite);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = new GameObject("TrainingArena");
            GameObject environmentRoot = CreateChild(root.transform, "Environment");
            GameObject actorsRoot = CreateChild(root.transform, "Actors");
            GameObject systemsRoot = CreateChild(root.transform, "Systems");
            GameObject templatesRoot = CreateChild(root.transform, "Templates");
            GameObject projectilesRoot = CreateChild(actorsRoot.transform, "Projectiles");

            Vector2 arenaSize = new Vector2(48f, 36f);
            const float wallThickness = 1.6f;

            ArenaBounds arenaBounds = new GameObject("ArenaBounds").AddComponent<ArenaBounds>();
            arenaBounds.transform.SetParent(environmentRoot.transform, false);
            arenaBounds.Configure(arenaSize, 2.5f);

            CreateFloor(environmentRoot.transform, floorSprite, arenaSize);
            CreateBoundaryWalls(environmentRoot.transform, squareSprite, arenaSize, wallThickness);
            CreateObstacleField(environmentRoot.transform, squareSprite, TrainingArenaLayout);

            Projectile projectileTemplate = CreateProjectileTemplate(templatesRoot.transform, circleSprite);
            TrainingTargetController trainingTarget = CreateTrainingTarget(actorsRoot.transform, circleSprite, arenaBounds);
            EnemyMlAgent enemyAgent = CreateMlEnemyTemplate(
                actorsRoot.transform,
                "TrainingEnemyAgent",
                circleSprite,
                squareSprite,
                projectileTemplate,
                projectilesRoot.transform,
                arenaBounds,
                trainingTarget.transform,
                EnemyAgentExecutionMode.Training,
                null,
                null).GetComponent<EnemyMlAgent>();

            TrainingArenaManager trainingManager = new GameObject("TrainingArenaManager").AddComponent<TrainingArenaManager>();
            trainingManager.transform.SetParent(systemsRoot.transform, false);
            trainingManager.Configure(arenaBounds, enemyAgent, trainingTarget, 2f, 0.9f, 12f, 64);

            enemyAgent.SetTrainingArena(trainingManager);
            enemyAgent.Configure(trainingTarget.transform, 30f, 14f, 8.5f, 4.5f, 0f, 0f, 0.65f, null, "TankArenaEnemy", 5, EnemyAgentExecutionMode.Training, trainingManager);
            enemyAgent.MaxStep = 1200;

            trainingTarget.Configure(arenaBounds, enemyAgent.transform);
            trainingManager.ResetEpisode(enemyAgent);

            projectileTemplate.gameObject.SetActive(false);
            CreateStaticCamera(Vector3.back * 10f, 20f);

            EditorSceneManager.MarkSceneDirty(scene);
            EnsureDirectory(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void ValidateSprites(Sprite floorSprite, Sprite circleSprite, Sprite squareSprite)
        {
            if (floorSprite == null || circleSprite == null || squareSprite == null)
            {
                throw new FileNotFoundException("Missing required sprite assets for Tank Arena scenes.");
            }
        }

        private static void CreateFloor(Transform parent, Sprite sprite, Vector2 arenaSize)
        {
            GameObject floor = CreateSpriteObject(parent, "Floor", sprite, Color.white, 0);
            floor.transform.localScale = new Vector3(arenaSize.x, arenaSize.y, 1f);
        }

        private static void CreateBoundaryWalls(Transform parent, Sprite sprite, Vector2 arenaSize, float thickness)
        {
            GameObject wallsRoot = CreateChild(parent, "Walls");
            Color wallColor = new Color(0.14f, 0.14f, 0.14f, 1f);
            float halfWidth = arenaSize.x * 0.5f;
            float halfHeight = arenaSize.y * 0.5f;

            CreateBlock(wallsRoot.transform, "WallTop", sprite, new Vector2(0f, halfHeight), new Vector2(arenaSize.x + thickness * 2f, thickness), 0f, wallColor, 4, ArenaObstacleType.Boundary);
            CreateBlock(wallsRoot.transform, "WallBottom", sprite, new Vector2(0f, -halfHeight), new Vector2(arenaSize.x + thickness * 2f, thickness), 0f, wallColor, 4, ArenaObstacleType.Boundary);
            CreateBlock(wallsRoot.transform, "WallLeft", sprite, new Vector2(-halfWidth, 0f), new Vector2(thickness, arenaSize.y), 0f, wallColor, 4, ArenaObstacleType.Boundary);
            CreateBlock(wallsRoot.transform, "WallRight", sprite, new Vector2(halfWidth, 0f), new Vector2(thickness, arenaSize.y), 0f, wallColor, 4, ArenaObstacleType.Boundary);
        }

        private static void CreateObstacleField(Transform parent, Sprite sprite, ObstacleSpec[] layout)
        {
            GameObject obstaclesRoot = CreateChild(parent, "Obstacles");
            Color obstacleColor = new Color(0.72f, 0.72f, 0.72f, 1f);

            for (int index = 0; index < layout.Length; index++)
            {
                ObstacleSpec obstacle = layout[index];
                CreateBlock(
                    obstaclesRoot.transform,
                    $"Obstacle_{index + 1:00}",
                    sprite,
                    obstacle.Position,
                    obstacle.Size,
                    obstacle.Rotation,
                    obstacleColor,
                    3,
                    ArenaObstacleType.Cover);
            }
        }

        private static Projectile CreateProjectileTemplate(Transform parent, Sprite sprite)
        {
            GameObject projectileRoot = CreateSpriteObject(parent, "ProjectileTemplate", sprite, new Color(1f, 0.79f, 0.2f, 1f), 8);
            projectileRoot.transform.localScale = Vector3.one * 0.26f;

            Rigidbody2D rb = projectileRoot.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = projectileRoot.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = false;

            Projectile projectile = projectileRoot.AddComponent<Projectile>();
            projectile.Configure(23f, 2.1f, 22f);
            return projectile;
        }

        private static PlayerController CreatePlayer(
            Transform parent,
            Sprite circleSprite,
            Sprite squareSprite,
            Projectile projectileTemplate,
            Transform projectileContainer,
            ArenaBounds arenaBounds)
        {
            GameObject root = new GameObject("Player");
            root.transform.SetParent(parent, false);

            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.65f;

            FactionMember faction = root.AddComponent<FactionMember>();
            faction.SetFaction(Faction.Player);

            Health health = root.AddComponent<Health>();
            health.Configure(120f, false);

            TankMovement2D movement = root.AddComponent<TankMovement2D>();
            movement.Configure(8.3f, 44f, 48f, arenaBounds, 0.75f);

            CreateVisual(root.transform, "Body", circleSprite, new Color(0.13f, 0.63f, 1f, 1f), new Vector2(1.45f, 1.45f), 5);
            Transform turret = CreateVisual(root.transform, "Turret", squareSprite, new Color(0.92f, 0.95f, 1f, 1f), new Vector2(1.25f, 0.34f), 6);
            Transform muzzle = CreateChild(turret, "Muzzle").transform;
            muzzle.localPosition = new Vector3(0.88f, 0f, 0f);

            TurretAim turretAim = root.AddComponent<TurretAim>();
            turretAim.SetTurret(turret);

            Weapon weapon = root.AddComponent<Weapon>();
            weapon.Configure(projectileTemplate, muzzle, 0.18f, 24f, 2f, 24f, projectileContainer);

            return root.AddComponent<PlayerController>();
        }

        private static GameObject CreateRulesEnemyTemplate(
            Transform parent,
            string objectName,
            Sprite circleSprite,
            Sprite squareSprite,
            Projectile projectileTemplate,
            Transform projectileContainer,
            ArenaBounds arenaBounds,
            Transform playerTarget)
        {
            GameObject root = CreateEnemyBase(parent, objectName, circleSprite, squareSprite, projectileTemplate, projectileContainer, arenaBounds);
            TankPerception2D perception = root.AddComponent<TankPerception2D>();
            perception.Configure(null, 16, 14f, 30f, true);

            EnemyAI enemyAI = root.AddComponent<EnemyAI>();
            enemyAI.Configure(playerTarget, 30f, 14f, 8.5f, 4.5f, 0f, 0f, 0.7f);
            return root;
        }

        private static GameObject CreateMlEnemyTemplate(
            Transform parent,
            string objectName,
            Sprite circleSprite,
            Sprite squareSprite,
            Projectile projectileTemplate,
            Transform projectileContainer,
            ArenaBounds arenaBounds,
            Transform playerTarget,
            EnemyAgentExecutionMode mode,
            ModelAsset model,
            TrainingArenaManager trainingArena)
        {
            GameObject root = CreateEnemyBase(parent, objectName, circleSprite, squareSprite, projectileTemplate, projectileContainer, arenaBounds);
            TankPerception2D perception = root.AddComponent<TankPerception2D>();
            perception.Configure(null, 16, 14f, 30f, true);

            root.AddComponent<Unity.MLAgents.Policies.BehaviorParameters>();
            root.AddComponent<Unity.MLAgents.DecisionRequester>();

            EnemyMlAgent agent = root.AddComponent<EnemyMlAgent>();
            agent.Configure(playerTarget, 30f, 14f, 8.5f, 4.5f, 0f, 0f, 0.7f, model, "TankArenaEnemy", 5, mode, trainingArena);
            agent.MaxStep = mode == EnemyAgentExecutionMode.Training ? 1200 : 0;
            return root;
        }

        private static GameObject CreateEnemyBase(
            Transform parent,
            string objectName,
            Sprite circleSprite,
            Sprite squareSprite,
            Projectile projectileTemplate,
            Transform projectileContainer,
            ArenaBounds arenaBounds)
        {
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(parent, false);

            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.62f;

            FactionMember faction = root.AddComponent<FactionMember>();
            faction.SetFaction(Faction.Enemy);

            Health health = root.AddComponent<Health>();
            health.Configure(75f, false);

            TankMovement2D movement = root.AddComponent<TankMovement2D>();
            movement.Configure(6.6f, 34f, 38f, arenaBounds, 0.72f);

            CreateVisual(root.transform, "Body", circleSprite, new Color(0.93f, 0.28f, 0.22f, 1f), new Vector2(1.34f, 1.34f), 5);
            Transform turret = CreateVisual(root.transform, "Turret", squareSprite, new Color(1f, 0.89f, 0.82f, 1f), new Vector2(1.1f, 0.3f), 6);
            Transform muzzle = CreateChild(turret, "Muzzle").transform;
            muzzle.localPosition = new Vector3(0.8f, 0f, 0f);

            TurretAim turretAim = root.AddComponent<TurretAim>();
            turretAim.SetTurret(turret);

            Weapon weapon = root.AddComponent<Weapon>();
            weapon.Configure(projectileTemplate, muzzle, 0.48f, 18.5f, 1.8f, 16f, projectileContainer);
            return root;
        }

        private static TrainingTargetController CreateTrainingTarget(Transform parent, Sprite circleSprite, ArenaBounds arenaBounds)
        {
            GameObject root = new GameObject("TrainingTarget");
            root.transform.SetParent(parent, false);

            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.65f;

            FactionMember faction = root.AddComponent<FactionMember>();
            faction.SetFaction(Faction.Player);

            Health health = root.AddComponent<Health>();
            health.Configure(120f, false);

            TankMovement2D movement = root.AddComponent<TankMovement2D>();
            movement.Configure(7.6f, 36f, 40f, arenaBounds, 0.75f);

            TankPerception2D perception = root.AddComponent<TankPerception2D>();
            perception.Configure(null, 12, 12f, 20f, true);

            CreateVisual(root.transform, "Body", circleSprite, new Color(0.13f, 0.63f, 1f, 1f), new Vector2(1.4f, 1.4f), 5);

            return root.AddComponent<TrainingTargetController>();
        }

        private static Camera CreateFollowCamera(Transform target, ArenaBounds arenaBounds, float size)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();

            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
            camera.transform.position = new Vector3(target.position.x, target.position.y, -10f);

            CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
            follow.Configure(target, arenaBounds, size, 0.08f);
            return camera;
        }

        private static Camera CreateStaticCamera(Vector3 position, float size)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();

            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
            camera.transform.position = position;
            return camera;
        }

        private static GameObject CreateBlock(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 localPosition,
            Vector2 localScale,
            float zRotation,
            Color color,
            int sortingOrder,
            ArenaObstacleType obstacleType)
        {
            GameObject block = CreateSpriteObject(parent, name, sprite, color, sortingOrder);
            block.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            block.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);
            block.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);

            BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            ArenaObstacle obstacle = block.AddComponent<ArenaObstacle>();
            obstacle.Configure(obstacleType);
            return block;
        }

        private static Transform CreateVisual(Transform parent, string name, Sprite sprite, Color color, Vector2 scale, int sortingOrder)
        {
            GameObject visual = CreateSpriteObject(parent, name, sprite, color, sortingOrder);
            visual.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            return visual.transform;
        }

        private static GameObject CreateSpriteObject(Transform parent, string name, Sprite sprite, Color color, int sortingOrder)
        {
            GameObject gameObject = CreateChild(parent, name);
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return gameObject;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void EnsureDirectory(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);

            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            string absoluteDirectory = Path.Combine(Directory.GetCurrentDirectory(), directory);

            if (!Directory.Exists(absoluteDirectory))
            {
                Directory.CreateDirectory(absoluteDirectory);
            }
        }

        private static void ApplyBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(MainScenePath, true),
                new EditorBuildSettingsScene(SampleScenePath, true),
                new EditorBuildSettingsScene(TrainingScenePath, true)
            };
        }

        private readonly struct ObstacleSpec
        {
            public ObstacleSpec(Vector2 position, Vector2 size, float rotation)
            {
                Position = position;
                Size = size;
                Rotation = rotation;
            }

            public Vector2 Position { get; }
            public Vector2 Size { get; }
            public float Rotation { get; }
        }
    }
}
