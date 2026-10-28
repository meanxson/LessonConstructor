using System.Collections.Generic;
using System.IO;
using System.Linq;
using App.Code.Domain.Execution;
using App.Code.Presentation.Analysis;
using App.Code.Presentation.Demo;
using App.Code.Presentation.Events;
using App.Code.Presentation.Logging;
using App.Code.Presentation.Modes;
using App.Code.Presentation.Runner;
using App.Code.Presentation.State;
using App.Code.Presentation.Steps;
using App.Code.Presentation.UI.Hud;
using App.Code.Presentation.UI.Hud.Views;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace App.Editor.Setup
{
    public static class DemoSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/LessonDemo.unity";
        private const string ConfigFolder = "Assets/App/Content/Config";
        private const string MaterialFolder = "Assets/App/Content/Materials";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.13f, 0.88f);
        private static readonly Color TextColor = new Color(0.93f, 0.95f, 0.98f);

        private static TMP_FontAsset _font;
        private static DefaultControls.Resources _resources;

        [MenuItem("Lesson Constructor/Build Demo Scene")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Debug.Log($"[DemoSceneBuilder] {Build()}");
        }

        public static string Build()
        {
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (_font == null) return $"TextMeshPro font not found at {FontPath}: import TMP Essential Resources first.";
            _resources = new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
            };

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var config = CreateConfig();

            CreateEnvironment();
            var player = CreatePlayer();
            var world = CreateWorld(player);
            var ui = CreateUi();
            var power = CreatePower(ui.PowerToggle);
            var steps = CreateSteps(world, ui, power);
            var runner = CreateScenario(steps, config);
            ConfigureHud(ui, runner, LoadOrCreate<HudTexts>("HudTexts"));

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            return $"Demo scene saved to {ScenePath}";
        }

        private sealed class Config
        {
            public TrainingPolicyAsset Training;
            public ExamPolicyAsset Exam;
            public AttemptScoreAnalyzerAsset AttemptAnalyzer;
            public PenaltyAnalyzerAsset PenaltyAnalyzer;
            public ConsoleLoggerAsset ConsoleLogger;
            public JsonFileLoggerAsset FileLogger;
        }

        private sealed class World
        {
            public TriggerEventSource ZoneA;
            public TriggerEventSource ZoneB;
            public TriggerEventSource ZoneC;
            public TriggerEventSource ZoneD;
            public TriggerEventSource DangerZone;
            public TriggerEventSource LoadZone;
            public CollisionEventSource Bell;
        }

        private sealed class Ui
        {
            public HudCompositionRoot Root;
            public TasksWindow Tasks;
            public MessageWindow Message;
            public ControlPanelWindow ControlPanel;
            public ResultWindow Result;
            public JournalWindow Journal;
            public Toggle PowerToggle;
            public readonly Dictionary<string, ButtonEventSource> Buttons = new Dictionary<string, ButtonEventSource>();
            public TextMeshProUGUI TasksText;
            public TextMeshProUGUI MessageText;
            public GameObject MessagePanel;
            public TextMeshProUGUI ResultText;
            public GameObject ResultPanel;
            public TextMeshProUGUI JournalText;
            public GameObject JournalPanel;
            public ScrollRect JournalScroll;
            public RectTransform ModeButtons;
            public RectTransform AnalyzerButtons;
            public Button Template;
            public Button Restart;
            public Button JournalToggle;
            public Button LoadFile;
            public Button HideResult;
        }

        private sealed class Power
        {
            public StateFlag Flag;
            public UnityEventSource TurnedOn;
            public UnityEventSource TurnedOff;
        }

        private static Config CreateConfig()
        {
            Directory.CreateDirectory(ConfigFolder);
            var config = new Config
            {
                Training = LoadOrCreate<TrainingPolicyAsset>("TrainingPolicy"),
                Exam = LoadOrCreate<ExamPolicyAsset>("ExamPolicy"),
                AttemptAnalyzer = LoadOrCreate<AttemptScoreAnalyzerAsset>("AttemptScoreAnalyzer"),
                PenaltyAnalyzer = LoadOrCreate<PenaltyAnalyzerAsset>("PenaltyAnalyzer"),
                ConsoleLogger = LoadOrCreate<ConsoleLoggerAsset>("ConsoleLogger"),
                FileLogger = LoadOrCreate<JsonFileLoggerAsset>("JsonFileLogger")
            };
            EditorAssign.Set(config.AttemptAnalyzer, ("displayName", "баллы за попытки"));
            EditorAssign.Set(config.PenaltyAnalyzer, ("displayName", "штрафы экзамена"));
            return config;
        }

        private static T LoadOrCreate<T>(string name) where T : ScriptableObject
        {
            var path = $"{ConfigFolder}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void CreateEnvironment()
        {
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
            camera.fieldOfView = 50f;
            camera.transform.position = new Vector3(0f, 19f, -15.5f);
            camera.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            camera.gameObject.AddComponent<AudioListener>();

            var floor = Primitive(PrimitiveType.Cube, "Floor", null, new Vector3(0f, -0.25f, 0f), new Vector3(26f, 0.5f, 18f), Material("Floor", new Color(0.32f, 0.35f, 0.39f)));
            floor.isStatic = true;

            var wallMaterial = Material("Wall", new Color(0.22f, 0.24f, 0.28f));
            Primitive(PrimitiveType.Cube, "Wall North", null, new Vector3(0f, 0.75f, 9.25f), new Vector3(26f, 1.5f, 0.5f), wallMaterial);
            Primitive(PrimitiveType.Cube, "Wall South", null, new Vector3(0f, 0.75f, -9.25f), new Vector3(26f, 1.5f, 0.5f), wallMaterial);
            Primitive(PrimitiveType.Cube, "Wall West", null, new Vector3(-13.25f, 0.75f, 0f), new Vector3(0.5f, 1.5f, 18f), wallMaterial);
            Primitive(PrimitiveType.Cube, "Wall East", null, new Vector3(13.25f, 0.75f, 0f), new Vector3(0.5f, 1.5f, 18f), wallMaterial);
        }

        private static GameObject CreatePlayer()
        {
            var player = Primitive(PrimitiveType.Capsule, "Player", null, new Vector3(0f, 1f, -3f), Vector3.one, Material("Player", new Color(0.2f, 0.55f, 1f)));
            player.tag = "Player";
            var body = player.AddComponent<Rigidbody>();
            body.mass = 2f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            player.AddComponent<DemoPlayerController>();
            player.AddComponent<ResettableTransform>();
            Label(player.transform, "Вы (WASD)", new Vector3(0f, 1.6f, 0f));
            return player;
        }

        private static World CreateWorld(GameObject player)
        {
            var root = new GameObject("World").transform;
            var playerFilter = new ActorFilter(null, "Player");

            var crate = Primitive(PrimitiveType.Cube, "Crate", root, new Vector3(5f, 0.6f, 1f), Vector3.one * 1.2f, Material("Crate", new Color(0.55f, 0.36f, 0.18f)));
            var crateBody = crate.AddComponent<Rigidbody>();
            crateBody.mass = 3f;
            crateBody.linearDamping = 2f;
            crateBody.angularDamping = 2f;
            crate.AddComponent<ResettableTransform>();
            Label(crate.transform, "Ящик", new Vector3(0f, 1.1f, 0f));

            var bell = Primitive(PrimitiveType.Cube, "Bell", root, new Vector3(9f, 0.8f, -5f), new Vector3(1.2f, 1.6f, 1.2f), Material("Bell", new Color(1f, 0.82f, 0.2f)));
            var bellSource = bell.AddComponent<CollisionEventSource>();
            bellSource.Configure(playerFilter);
            EditorAssign.Set(bellSource, ("label", "Колокол"));
            Label(bell.transform, "Колокол", new Vector3(0f, 1.4f, 0f));

            return new World
            {
                ZoneA = Zone(root, "Zone A", "A: инструктаж", new Vector3(-9f, 0f, -5f), new Vector2(3.5f, 3.5f), new Color(0.25f, 0.55f, 1f), playerFilter),
                ZoneB = Zone(root, "Zone B", "B: ворота 1", new Vector3(-6.5f, 0f, 5f), new Vector2(3f, 3f), new Color(0.2f, 0.8f, 0.4f), playerFilter),
                ZoneC = Zone(root, "Zone C", "C: ворота 2", new Vector3(-1.5f, 0f, 7f), new Vector2(3f, 2.5f), new Color(0.2f, 0.8f, 0.4f), playerFilter),
                ZoneD = Zone(root, "Zone D", "D: пост контроля", new Vector3(10f, 0f, -0.5f), new Vector2(3f, 3f), new Color(0.65f, 0.35f, 0.95f), playerFilter),
                DangerZone = Zone(root, "Danger Zone", "Опасная зона", new Vector3(1.5f, 0f, 3.5f), new Vector2(3.5f, 2.5f), new Color(0.95f, 0.2f, 0.2f), playerFilter),
                LoadZone = Zone(root, "Load Zone", "Погрузка ящика", new Vector3(9f, 0f, 6f), new Vector2(3.5f, 3f), new Color(1f, 0.55f, 0.1f), new ActorFilter(crate, string.Empty)),
                Bell = bellSource
            };
        }

        private static TriggerEventSource Zone(Transform parent, string name, string caption, Vector3 position, Vector2 size, Color color, ActorFilter filter)
        {
            var zone = new GameObject(name);
            zone.transform.SetParent(parent, false);
            zone.transform.position = position;
            var trigger = zone.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1f, 0f);
            trigger.size = new Vector3(size.x, 2f, size.y);

            var visual = Primitive(PrimitiveType.Cube, "Visual", zone.transform, Vector3.zero, new Vector3(size.x, 0.04f, size.y), Material(name.Replace(" ", string.Empty), color));
            visual.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            var source = zone.AddComponent<TriggerEventSource>();
            source.Configure(filter);
            EditorAssign.Set(source, ("label", caption));
            Label(zone.transform, caption, new Vector3(0f, 0.6f, 0f));
            return source;
        }

        private static Ui CreateUi()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var ui = new Ui();
            var canvasObject = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.transform;
            ui.Root = canvasObject.AddComponent<HudCompositionRoot>();

            ui.Template = Button(root, "Button", 250f);
            ui.Template.gameObject.name = "Button Template";
            ui.Template.gameObject.SetActive(false);

            var tasks = Panel(root, "Tasks Window", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(500f, 320f));
            ui.TasksText = Text(tasks, "Tasks", string.Empty, 20, TextAlignmentOptions.TopLeft);
            Stretch(ui.TasksText.rectTransform, 18f);
            ui.Tasks = tasks.gameObject.AddComponent<TasksWindow>();

            var controls = Panel(root, "Control Panel Window", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(560f, 150f));
            var controlsTitle = Text(controls, "Title", "Режим прохождения", 20, TextAlignmentOptions.TopLeft);
            Place(controlsTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-32f, 28f));
            ui.ModeButtons = Row(controls, "Mode Buttons", new Vector2(0f, -44f));
            var actions = Row(controls, "Run Buttons", new Vector2(0f, -96f));
            ui.Restart = Button(actions, "Перезапустить", 200f);
            ui.JournalToggle = Button(actions, "Журнал", 140f);
            ui.ControlPanel = controls.gameObject.AddComponent<ControlPanelWindow>();

            var messageWindow = Layer(root, "Message Window");
            var message = Panel(messageWindow, "Panel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-40f, -20f), new Vector2(700f, 120f));
            message.GetComponent<Image>().color = new Color(0.6f, 0.1f, 0.12f, 0.92f);
            ui.MessagePanel = message.gameObject;
            ui.MessageText = Text(message, "Message", string.Empty, 22, TextAlignmentOptions.Center);
            Stretch(ui.MessageText.rectTransform, 14f);
            ui.Message = messageWindow.gameObject.AddComponent<MessageWindow>();

            var console = Panel(root, "Console Panel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1180f, 150f));
            var consoleTitle = Text(console, "Title", "Пульт: кнопки — события UI Button, тумблер — UnityEvent", 18, TextAlignmentOptions.TopLeft);
            Place(consoleTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-32f, 26f));
            var consoleRow = Row(console, "Console Buttons", new Vector2(0f, -48f));
            ui.PowerToggle = Toggle(consoleRow, "Питание", 150f);
            foreach (var (key, caption, width) in new[]
                     {
                         ("helmet", "Надеть каску", 170f), ("start", "Запуск", 120f), ("check", "Проверить", 140f),
                         ("signal", "Сигнал", 120f), ("finish", "Завершить смену", 200f), ("emergency", "Аварийный стоп", 190f)
                     })
            {
                var button = Button(consoleRow, caption, width);
                if (key == "emergency") button.GetComponent<Image>().color = new Color(1f, 0.45f, 0.45f);
                var source = button.gameObject.AddComponent<ButtonEventSource>();
                EditorAssign.Set(source, ("label", caption));
                ui.Buttons.Add(key, source);
            }

            var resultWindow = Layer(root, "Result Window");
            var result = Panel(resultWindow, "Panel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -190f), new Vector2(620f, 690f));
            result.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 0.96f);
            ui.ResultPanel = result.gameObject;
            ui.ResultText = Text(result, "Result", string.Empty, 17, TextAlignmentOptions.TopLeft);
            Place(ui.ResultText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(-32f, -150f));
            ui.AnalyzerButtons = Row(result, "Analyzer Buttons", Vector2.zero);
            Place(ui.AnalyzerButtons, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(-32f, 46f));
            var resultActions = Row(result, "Result Actions", Vector2.zero);
            Place(resultActions, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(-32f, 46f));
            ui.LoadFile = Button(resultActions, "Загрузить журнал из файла", 330f);
            ui.HideResult = Button(resultActions, "Скрыть", 120f);
            ui.Result = resultWindow.gameObject.AddComponent<ResultWindow>();

            var journalWindow = Layer(root, "Journal Window");
            var journal = Panel(journalWindow, "Panel", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 190f), new Vector2(1000f, 340f));
            journal.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.94f);
            ui.JournalPanel = journal.gameObject;
            var scrollObject = DefaultControls.CreateScrollView(_resources);
            scrollObject.name = "Journal Scroll";
            scrollObject.transform.SetParent(journal, false);
            Stretch((RectTransform)scrollObject.transform, 10f);
            scrollObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            ui.JournalScroll = scrollObject.GetComponent<ScrollRect>();
            ui.JournalScroll.horizontal = false;
            Object.DestroyImmediate(ui.JournalScroll.horizontalScrollbar.gameObject);
            var content = ui.JournalScroll.content;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ui.JournalText = Text(content, "Journal", string.Empty, 15, TextAlignmentOptions.TopLeft);
            ui.JournalText.textWrappingMode = TextWrappingModes.Normal;
            ui.Journal = journalWindow.gameObject.AddComponent<JournalWindow>();

            return ui;
        }
        private static Power CreatePower(Toggle toggle)
        {
            var root = new GameObject("Power");
            var power = new Power
            {
                Flag = root.AddComponent<StateFlag>(),
                TurnedOn = new GameObject("Power On Event").AddComponent<UnityEventSource>(),
                TurnedOff = new GameObject("Power Off Event").AddComponent<UnityEventSource>()
            };
            power.TurnedOn.transform.SetParent(root.transform, false);
            power.TurnedOff.transform.SetParent(root.transform, false);
            EditorAssign.Set(power.Flag, ("label", "Питание"), ("initialValue", false));
            EditorAssign.Set(power.TurnedOn, ("label", "Питание включено"));
            EditorAssign.Set(power.TurnedOff, ("label", "Питание выключено"));

            UnityEventTools.AddPersistentListener(toggle.onValueChanged, new UnityAction<bool>(power.Flag.Set));
            UnityEventTools.AddVoidPersistentListener(power.Flag.OnTurnedOn, power.TurnedOn.Emit);
            UnityEventTools.AddVoidPersistentListener(power.Flag.OnTurnedOff, power.TurnedOff.Emit);
            EditorUtility.SetDirty(toggle);
            EditorUtility.SetDirty(power.Flag);

            EditorAssign.Set(toggle.gameObject.AddComponent<ToggleStateView>(), ("flag", power.Flag));
            return power;
        }

        private static Step[] CreateSteps(World world, Ui ui, Power power)
        {
            var root = new GameObject("Steps").transform;
            var emergency = new ScenarioEventSource[] { ui.Buttons["emergency"] };
            var danger = new ScenarioEventSource[] { world.DangerZone };
            const string emergencyMessage = "Аварийный стоп нажат без причины";
            const string dangerMessage = "Вы вошли в опасную зону";

            ActionStep Action(string id, string instruction, ScenarioEventSource action, ScenarioEventSource[] violations, string violationMessage)
            {
                var step = StepObject<ActionStep>(root, id, instruction, violations, violationMessage);
                EditorAssign.Set(step, ("action", action));
                return step;
            }

            var step1 = Action("step_01", "Нажмите «Надеть каску» на пульте", ui.Buttons["helmet"], emergency, emergencyMessage);
            var step2 = Action("step_02", "Встаньте в синюю зону A (инструктаж)", world.ZoneA, danger, dangerMessage);
            var step3 = Action("step_03", "Толкните жёлтый колокол (столкновение)", world.Bell, new ScenarioEventSource[0], string.Empty);
            var step4 = Action("step_04", "Включите тумблер «Питание»", power.TurnedOn, new ScenarioEventSource[0], string.Empty);

            var step5 = StepObject<StateGuardedStep>(root, "step_05", "Нажмите «Запуск» — только при включённом питании", emergency, emergencyMessage);
            EditorAssign.Set(step5, ("action", ui.Buttons["start"]), ("requiredState", power.Flag), ("requiredValue", true),
                ("wrongStateMessage", "Запуск при выключенном питании запрещён"));

            var step6 = StepObject<SequenceStep>(root, "step_06", "Пройдите ворота: сначала зелёная зона B, затем C", danger, dangerMessage);
            EditorAssign.Set(step6, ("sequence", new ScenarioEventSource[] { world.ZoneB, world.ZoneC }),
                ("wrongOrderMessage", "Нарушен порядок: сначала зона B, потом C"));

            var step7 = Action("step_07", "Задвиньте ящик в оранжевую зону погрузки", world.LoadZone, danger, dangerMessage);
            var step8 = Action("step_08", "Нажмите «Проверить»", ui.Buttons["check"], emergency, emergencyMessage);
            var step9 = Action("step_09", "Выключите тумблер «Питание»", power.TurnedOff, new ScenarioEventSource[0], string.Empty);
            var step10 = Action("step_10", "Нажмите «Сигнал»", ui.Buttons["signal"], emergency, emergencyMessage);

            var step11 = StepObject<StateGuardedStep>(root, "step_11", "Встаньте на фиолетовый пост D — питание должно быть выключено", danger, dangerMessage);
            EditorAssign.Set(step11, ("action", world.ZoneD), ("requiredState", power.Flag), ("requiredValue", false),
                ("wrongStateMessage", "На пост D можно только при выключенном питании"));

            var step12 = Action("step_12", "Нажмите «Завершить смену»", ui.Buttons["finish"], emergency, emergencyMessage);
            return new Step[] { step1, step2, step3, step4, step5, step6, step7, step8, step9, step10, step11, step12 };
        }

        private static T StepObject<T>(Transform parent, string id, string instruction, ScenarioEventSource[] violations, string violationMessage) where T : Step
        {
            var step = new GameObject($"{id} ({typeof(T).Name})").AddComponent<T>();
            step.transform.SetParent(parent, false);
            EditorAssign.Set(step, ("stepId", id), ("instruction", instruction), ("violationSources", violations), ("violationMessage", violationMessage));
            return step;
        }

        private static ScenarioRunner CreateScenario(Step[] steps, Config config)
        {
            var root = new GameObject("Scenario");
            var modes = new GameObject("Modes").transform;
            modes.SetParent(root.transform, false);
            var with = StepActivation.TogetherWithPrevious;
            var after = StepActivation.AfterPrevious;
            StepEntry E(int number, StepActivation activation) => new StepEntry(steps[number - 1], activation);

            var training = Mode(modes, "Тренировка", config.Training, config.AttemptAnalyzer, OutOfTurnBehaviour.RecordViolation,
                new StepGroupSettings("Все шаги", new[]
                {
                    E(1, after), E(2, with), E(3, with), E(4, with), E(5, after), E(6, after), E(7, after), E(8, after),
                    E(9, after), E(10, with), E(11, after), E(12, with)
                }));
            var exam = Mode(modes, "Экзамен", config.Exam, config.PenaltyAnalyzer, OutOfTurnBehaviour.RecordViolation,
                new StepGroupSettings("Подготовка", new[] { E(1, after), E(2, with), E(3, with), E(4, with) }),
                new StepGroupSettings("Запуск", new[] { E(5, after), E(6, after), E(7, after), E(8, after) }),
                new StepGroupSettings("Завершение", new[] { E(9, after), E(10, with), E(11, after), E(12, with) }));
            var free = Mode(modes, "Свободная тренировка", config.Training, config.AttemptAnalyzer, OutOfTurnBehaviour.Ignore,
                new StepGroupSettings("Все шаги", steps.Select(step => new StepEntry(step, with))));

            var runner = root.AddComponent<ScenarioRunner>();
            EditorAssign.Set(runner,
                ("modes", new Object[] { training, exam, free }),
                ("defaultModeIndex", 0),
                ("startOnPlay", true),
                ("loggers", new Object[] { config.FileLogger, config.ConsoleLogger }),
                ("analyzers", new Object[] { config.AttemptAnalyzer, config.PenaltyAnalyzer }));
            return runner;
        }

        private static ScenarioMode Mode(Transform parent, string name, ExecutionPolicyAsset policy, AnalyzerAsset analyzer,
            OutOfTurnBehaviour outOfTurn, params StepGroupSettings[] groups)
        {
            var mode = new GameObject($"Mode: {name}").AddComponent<ScenarioMode>();
            mode.transform.SetParent(parent, false);
            EditorAssign.Set(mode, ("displayName", name), ("policy", policy), ("analyzer", analyzer), ("outOfTurn", outOfTurn));

            var serialized = new SerializedObject(mode);
            var groupsProperty = serialized.FindProperty("groups");
            groupsProperty.arraySize = groups.Length;
            for (var groupIndex = 0; groupIndex < groups.Length; groupIndex++)
            {
                var groupProperty = groupsProperty.GetArrayElementAtIndex(groupIndex);
                groupProperty.FindPropertyRelative("name").stringValue = groups[groupIndex].Name;
                var entries = groupProperty.FindPropertyRelative("steps");
                entries.arraySize = groups[groupIndex].Steps.Count;
                for (var entryIndex = 0; entryIndex < entries.arraySize; entryIndex++)
                {
                    var entry = entries.GetArrayElementAtIndex(entryIndex);
                    entry.FindPropertyRelative("step").objectReferenceValue = groups[groupIndex].Steps[entryIndex].Step;
                    entry.FindPropertyRelative("activation").enumValueIndex = (int)groups[groupIndex].Steps[entryIndex].Activation;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return mode;
        }

        private static void ConfigureHud(Ui ui, ScenarioRunner runner, HudTexts texts)
        {
            EditorAssign.Set(texts,
                ("activeTasks", "Активные задания"),
                ("mode", "Режим"),
                ("group", "Группа"),
                ("groupViolations", "Нарушений в группе"),
                ("progress", "Завершено шагов"),
                ("finished", "Сценарий завершён — результат справа"),
                ("notStarted", "Выберите режим справа сверху"),
                ("outOfTurn", "Действие вне очереди"),
                ("retry", "повторите шаг"),
                ("failed", "шаг провален"),
                ("result", "Результат"),
                ("score", "Итог"),
                ("attempt", "поп."),
                ("violations", "нар."),
                ("warnings", "Предупреждения анализатора"),
                ("journal", "Журнал событий"),
                ("journalSource", "Источник"),
                ("analyzeWith", "Оценить"),
                ("statusPending", "Ожидает"),
                ("statusActive", "Активен"),
                ("statusCompleted", "Выполнен"),
                ("statusFailed", "Провален"),
                ("statusSkipped", "Пропущен"));

            EditorAssign.Set(ui.Tasks, ("texts", texts), ("label", ui.TasksText));
            EditorAssign.Set(ui.Message, ("texts", texts), ("panel", ui.MessagePanel), ("label", ui.MessageText));
            EditorAssign.Set(ui.ControlPanel, ("modeButtons", ui.ModeButtons), ("buttonTemplate", ui.Template),
                ("restartButton", ui.Restart), ("journalButton", ui.JournalToggle));
            EditorAssign.Set(ui.Result, ("texts", texts), ("panel", ui.ResultPanel), ("label", ui.ResultText),
                ("analyzerButtons", ui.AnalyzerButtons), ("buttonTemplate", ui.Template), ("loadFileButton", ui.LoadFile),
                ("hideButton", ui.HideResult));
            EditorAssign.Set(ui.Journal, ("texts", texts), ("panel", ui.JournalPanel), ("label", ui.JournalText),
                ("scroll", ui.JournalScroll));
            EditorAssign.Set(ui.Root, ("runner", runner), ("tasksWindow", ui.Tasks), ("messageWindow", ui.Message),
                ("controlPanelWindow", ui.ControlPanel), ("resultWindow", ui.Result), ("journalWindow", ui.Journal));
        }
        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.position = position;
            primitive.transform.localScale = scale;
            primitive.GetComponent<MeshRenderer>().sharedMaterial = material;
            return primitive;
        }

        private static Material Material(string name, Color color)
        {
            Directory.CreateDirectory(MaterialFolder);
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.2f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Label(Transform parent, string caption, Vector3 offset)
        {
            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(parent, false);
            label.transform.position = parent.position + offset;
            label.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            var lossy = parent.lossyScale;
            label.transform.localScale = new Vector3(1f / lossy.x, 1f / lossy.y, 1f / lossy.z);
            var text = label.AddComponent<TextMeshPro>();
            text.font = _font;
            text.text = caption;
            text.fontSize = 4.2f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.color = Color.white;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color32(0, 0, 0, 200);
            text.rectTransform.sizeDelta = new Vector2(8f, 1.5f);
        }

        private static RectTransform Layer(Transform parent, string name)
        {
            var layer = new GameObject(name, typeof(RectTransform));
            layer.transform.SetParent(parent, false);
            var rect = (RectTransform)layer.transform;
            Stretch(rect, 0f);
            return rect;
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var image = panel.GetComponent<Image>();
            image.sprite = _resources.background;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = PanelColor;
            var rect = (RectTransform)panel.transform;
            Place(rect, anchor, anchor, pivot, position, size);
            return rect;
        }

        private static RectTransform Row(Transform parent, string name, Vector2 position)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            var rect = (RectTransform)row.transform;
            Place(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), position, new Vector2(-32f, 46f));
            return rect;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string content, float size, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = TextColor;
            text.richText = true;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }

        private static Button Button(Transform parent, string caption, float width)
        {
            var buttonObject = DefaultControls.CreateButton(_resources);
            buttonObject.name = caption;
            buttonObject.transform.SetParent(parent, false);
            ReplaceLabel(buttonObject.GetComponentInChildren<Text>(), caption, new Color(0.1f, 0.12f, 0.16f), TextAlignmentOptions.Center);
            buttonObject.AddComponent<LayoutElement>().preferredWidth = width;
            ((RectTransform)buttonObject.transform).sizeDelta = new Vector2(width, 44f);
            return buttonObject.GetComponent<Button>();
        }

        private static Toggle Toggle(Transform parent, string caption, float width)
        {
            var toggleObject = DefaultControls.CreateToggle(_resources);
            toggleObject.name = $"Toggle {caption}";
            toggleObject.transform.SetParent(parent, false);
            var label = ReplaceLabel(toggleObject.GetComponentInChildren<Text>(), caption, TextColor, TextAlignmentOptions.Left);
            toggleObject.AddComponent<LayoutElement>().preferredWidth = width;
            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.isOn = false;
            var background = (RectTransform)toggle.targetGraphic.transform;
            background.anchoredPosition = new Vector2(10f, -23f);
            background.sizeDelta = new Vector2(26f, 26f);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(44f, 0f);
            labelRect.offsetMax = Vector2.zero;
            return toggle;
        }

        private static TextMeshProUGUI ReplaceLabel(Text legacy, string caption, Color color, TextAlignmentOptions alignment)
        {
            var host = legacy.gameObject;
            Object.DestroyImmediate(legacy);
            var label = host.AddComponent<TextMeshProUGUI>();
            label.font = _font;
            label.text = caption;
            label.fontSize = 19f;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 19f;
            return label;
        }
        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
