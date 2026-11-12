using System.Reflection;
using Godot;

namespace ECS.Utils;

/// <summary>
/// Reflection-based static wrapper around DialogueManager from the addon.
/// </summary>
internal static class DialogueManagerWrapper
{
    /// <summary>
    /// Emitted when a dialogue balloon is created and dialogue starts.
    /// </summary>
    public static event Action<Resource>? DialogueStarted
    {
        add => DialogueManagerWrapper.AddEventHandler(
            DialogueManagerWrapper._dialogueStartedEventInfo,
            DialogueManagerWrapper._dialogueStartedDelegates,
            (Resource resource) => value?.Invoke(resource),
            value);

        remove => DialogueManagerWrapper.RemoveEventHandler(
            DialogueManagerWrapper._dialogueStartedEventInfo,
            DialogueManagerWrapper._dialogueStartedDelegates,
            value);
    }

    /// <summary>
    /// Emitted when a cue is encountered while traversing dialogue, usually when jumping from a goto line.
    /// </summary>
    public static event Action<string>? PassedCue
    {
        add => DialogueManagerWrapper.AddEventHandler(
            DialogueManagerWrapper._passedCueEventInfo,
            DialogueManagerWrapper._passedCueDelegates,
            (string cue) => value?.Invoke(cue),
            value);

        remove => DialogueManagerWrapper.RemoveEventHandler(
            DialogueManagerWrapper._passedCueEventInfo,
            DialogueManagerWrapper._passedCueDelegates,
            value);
    }

    /// <summary>
    /// Emitted when a line of dialogue is encountered.
    /// </summary>
    public static event Action<DialogueLineWrapper>? GotDialogue
    {
        add => DialogueManagerWrapper.AddEventHandler(
            DialogueManagerWrapper._gotDialogueEventInfo,
            DialogueManagerWrapper._gotDialogueDelegates,
            (dynamic? dialogueLine) => value?.Invoke(DialogueManagerWrapper.ToDialogueLineWrapper(dialogueLine)),
            value);

        remove => DialogueManagerWrapper.RemoveEventHandler(
            DialogueManagerWrapper._gotDialogueEventInfo,
            DialogueManagerWrapper._gotDialogueDelegates,
            value);
    }

    /// <summary>
    /// Emitted when some dialogue has reached the end.
    /// </summary>
    public static event Action<Resource>? DialogueEnded
    {
        add => DialogueManagerWrapper.AddEventHandler(
            DialogueManagerWrapper._dialogueEndedEventInfo,
            DialogueManagerWrapper._dialogueEndedDelegates,
            (Resource resource) => value?.Invoke(resource),
            value);

        remove => DialogueManagerWrapper.RemoveEventHandler(
            DialogueManagerWrapper._dialogueEndedEventInfo,
            DialogueManagerWrapper._dialogueEndedDelegates,
            value);
    }

    /// <summary>
    /// Gets the singleton GodotObject instance of DialogueManager.
    /// </summary>
    public static GodotObject Instance =>
        (GodotObject)(DialogueManagerWrapper._instanceMethodInfo.Invoke(null, DialogueManagerWrapper._instanceParameters)
            ?? throw new InvalidOperationException("DialogueManager.Instance returned 'null'"));

    private static readonly EventInfo _dialogueStartedEventInfo;
    private static readonly Dictionary<Action<Resource>, Delegate> _dialogueStartedDelegates = [];

    private static readonly EventInfo _passedCueEventInfo;
    private static readonly Dictionary<Action<string>, Delegate> _passedCueDelegates = [];

    private static readonly EventInfo _gotDialogueEventInfo;
    private static readonly Dictionary<Action<DialogueLineWrapper>, Delegate> _gotDialogueDelegates = [];

    private static readonly EventInfo _dialogueEndedEventInfo;
    private static readonly Dictionary<Action<Resource>, Delegate> _dialogueEndedDelegates = [];

    private static readonly MethodInfo _showDialogueBalloonMethodInfo;
    private static readonly object?[] _showDialogueBalloonParameters = [null, "", null];

    private static readonly MethodInfo _getNextDialogueLineMethodInfo;
    private static readonly object?[] _getNextDialogueLineParameters = [null, "", null];

    private static readonly MethodInfo _instanceMethodInfo;
    private static readonly object?[] _instanceParameters = [];

    /// <summary>
    /// Resolves all required members of DialogueManager via reflection.
    /// </summary>
    static DialogueManagerWrapper()
    {
        var dialogueManagerType = Assembly
            .GetExecutingAssembly()
            .GetType("DialogueManagerRuntime.DialogueManager")
                ?? throw new InvalidOperationException("DialogueManager type not found");

        DialogueManagerWrapper._dialogueStartedEventInfo = dialogueManagerType
            .GetEvent("DialogueStarted", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("DialogueManager.DialogueStarted event not found");

        DialogueManagerWrapper._passedCueEventInfo = dialogueManagerType
            .GetEvent("PassedCue", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("DialogueManager.PassedCue event not found");

        DialogueManagerWrapper._gotDialogueEventInfo = dialogueManagerType
            .GetEvent("GotDialogue", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("DialogueManager.GotDialogue event not found");

        DialogueManagerWrapper._dialogueEndedEventInfo = dialogueManagerType
            .GetEvent("DialogueEnded", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("DialogueManager.DialogueEnded event not found");

        DialogueManagerWrapper._showDialogueBalloonMethodInfo = dialogueManagerType
            .GetMethod("ShowDialogueBalloon", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("DialogueManager.ShowDialogueBalloon method not found");

        DialogueManagerWrapper._getNextDialogueLineMethodInfo = dialogueManagerType
            .GetMethod("GetNextDialogueLine", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("DialogueManager.GetNextDialogueLine method not found");

        DialogueManagerWrapper._instanceMethodInfo = dialogueManagerType
            .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
            ?.GetGetMethod()
                ?? throw new InvalidOperationException("DialogueManager.Instance.get property not found");
    }

    private static void AddEventHandler<TRawInput, TConvertedInput>(
        EventInfo eventInfo,
        Dictionary<Action<TConvertedInput>, Delegate> delegates,
        Action<TRawInput> rawAction,
        Action<TConvertedInput>? convertedAction)
    {
        if (convertedAction == null)
            throw new InvalidOperationException($"DialogueManager.{eventInfo.Name} should receive a non-null action");

        if (!delegates.TryGetValue(convertedAction, out var handler))
        {
            if (eventInfo.EventHandlerType == null)
                throw new InvalidOperationException($"DialogueManager.{eventInfo.Name}.{nameof(eventInfo.EventHandlerType)} shouldn't be 'null'");

            handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, rawAction.Target, rawAction.Method);
        }

        delegates[convertedAction] = handler;
        eventInfo.AddEventHandler(null, delegates[convertedAction]);
    }

    private static void RemoveEventHandler<TConvertedInput>(
        EventInfo eventInfo,
        Dictionary<Action<TConvertedInput>, Delegate> delegates,
        Action<TConvertedInput>? convertedAction)
    {
        if (convertedAction == null)
            throw new InvalidOperationException($"DialogueManager.{eventInfo.Name} should receive a non-null action");

        if (!delegates.TryGetValue(convertedAction, out var handler))
            return;

        eventInfo.RemoveEventHandler(null, handler);
    }

    private static DialogueLineWrapper? ToDialogueLineWrapper(dynamic? dialogueLine) =>
        dialogueLine != null ? new()
        {
            Character = dialogueLine.Character,
            Text = dialogueLine.Text,
            NextId = dialogueLine.NextId,
            Responses = System.Linq.Enumerable.ToArray(
                System.Linq.Enumerable.Select(dialogueLine.Responses,
                    (Func<dynamic, int, DialogueManagerWrapper.DialogueResponseWrapper>)
                        DialogueManagerWrapper.ToDialogueResponseWrapper)),
            ExtraGameStates = dialogueLine.ExtraGameStates,
        } : null;

    private static DialogueResponseWrapper ToDialogueResponseWrapper(dynamic dialogueResponse, int index) =>
        new()
        {
            IsAllowed = dialogueResponse.IsAllowed,
            NextId = dialogueResponse.NextId,
        };

    /// <summary>
    /// Show the configured dialogue balloon
    /// </summary>
    public static Node ShowDialogueBalloon(Resource resource, string key = "", IReadOnlyCollection<Variant>? extraGameStates = null)
    {
        DialogueManagerWrapper._showDialogueBalloonParameters[0] = resource;
        DialogueManagerWrapper._showDialogueBalloonParameters[1] = key;
        DialogueManagerWrapper._showDialogueBalloonParameters[2] = extraGameStates;
        return (Node)(DialogueManagerWrapper._showDialogueBalloonMethodInfo.Invoke(null, DialogueManagerWrapper._showDialogueBalloonParameters)
            ?? throw new InvalidOperationException("DialogueManager.ShowDialogueBalloon returned 'null'"));
    }

    /// <summary>
    /// Step through lines and run any mutations until we either hit some dialogue or the end of the conversation.
    /// </summary>
    public static Task<DialogueLineWrapper?> GetNextDialogueLine(Resource resource, string key)
    {
        DialogueManagerWrapper._getNextDialogueLineParameters[0] = resource;
        DialogueManagerWrapper._getNextDialogueLineParameters[1] = key;
        return ((Task<dynamic?>?)DialogueManagerWrapper._getNextDialogueLineMethodInfo
            .Invoke(null, DialogueManagerWrapper._getNextDialogueLineParameters))
            ?.ContinueWith(DialogueManagerWrapper.ToDialogueLineWrapper)
                ?? Task.FromException<DialogueLineWrapper?>(new InvalidOperationException("DialogueManager.GetNextDialogueLine returned 'null'"));
    }

    /// <summary>
    /// Typed snapshot of a DialogueManager dialogue line for use in ECS.
    /// </summary>
    public readonly struct DialogueLineWrapper
    {
        /// <summary>
        /// The character name that is saying this line.
        /// </summary>
        public required string Character { get; init; }

        /// <summary>
        /// The dialogue being spoken.
        /// </summary>
        public required string Text { get; init; }

        /// <summary>
        /// The next line ID after this line.
        /// </summary>
        public required string NextId { get; init; }

        /// <summary>
        /// A list of responses attached to this line of dialogue.
        /// </summary>
        public required IReadOnlyCollection<DialogueResponseWrapper> Responses { get; init; }

        /// <summary>
        /// A list of any extra game states to check when resolving variables and mutations.
        /// </summary>
        public required IReadOnlyCollection<Variant> ExtraGameStates { get; init; }
    }

    /// <summary>
    /// Typed snapshot of a DialogueManager response option for use in ECS.
    /// </summary>
    public readonly struct DialogueResponseWrapper
    {
        /// <summary>
        /// The ID of this response
        /// </summary>
        public required string NextId { get; init; }

        /// <summary>
        /// <c>true</c> if the condition of this line was met.
        /// </summary>
        public required bool IsAllowed { get; init; }
    }
}