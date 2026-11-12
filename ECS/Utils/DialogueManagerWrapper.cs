using System.Reflection;
using Godot;

namespace ECS.Utils;

/// <summary>
/// // TODO: document this.
/// </summary>
internal static class DialogueManagerWrapper
{
    /// <summary>
    /// Emitted when a dialogue balloon is created and dialogue starts.
    /// </summary>
    public static event Action<Resource> DialogueStarted
    {
        add => DialogueManagerWrapper._dialogueStartedEventInfo.AddEventHandler(null, value);
        remove => DialogueManagerWrapper._dialogueStartedEventInfo.RemoveEventHandler(null, value);
    }

    /// <summary>
    /// Emitted when a line of dialogue is encountered.
    /// </summary>
    public static event Action<DialogueLineWrapper> GotDialogue
    {
        add
        {
            var converted = DialogueManagerWrapper._gotDialogueConversor
                .GetValueOrDefault(value)
                    ?? ((result) => value(DialogueManagerWrapper.ToDialogueLineWrapper(result)));

            DialogueManagerWrapper._gotDialogueConversor[value] = converted;
            DialogueManagerWrapper._gotDialogueEventInfo.AddEventHandler(null, converted);
        }
        remove
        {
            var converted = DialogueManagerWrapper._gotDialogueConversor.GetValueOrDefault(value);
            if (converted == null)
                return;

            DialogueManagerWrapper._gotDialogueEventInfo.RemoveEventHandler(null, converted);
        }
    }

    /// <summary>
    /// Emitted when some dialogue has reached the end.
    /// </summary>
    public static event Action<Resource> DialogueEnded
    {
        add => DialogueManagerWrapper._dialogueEndedEventInfo.AddEventHandler(null, value);
        remove => DialogueManagerWrapper._dialogueEndedEventInfo.RemoveEventHandler(null, value);
    }

    private static readonly EventInfo _dialogueStartedEventInfo;

    private static readonly EventInfo _gotDialogueEventInfo;
    private static readonly Dictionary<Action<DialogueLineWrapper>, Action<dynamic>> _gotDialogueConversor = [];

    private static readonly EventInfo _dialogueEndedEventInfo;

    private static readonly MethodInfo _showDialogueBalloonMethodInfo;
    private static readonly object?[] _showDialogueBalloonParameters = [null, "", null];

    private static readonly MethodInfo _getNextDialogueLineMethodInfo;
    private static readonly object?[] _getNextDialogueLineParameters = [null, "", null];

    /// <summary>
    /// // TODO:
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
    public static Node ShowDialogueBalloon(Resource resource)
    {
        DialogueManagerWrapper._showDialogueBalloonParameters[0] = resource;
        return (Node)DialogueManagerWrapper._showDialogueBalloonMethodInfo.Invoke(null, DialogueManagerWrapper._showDialogueBalloonParameters)!;
    }

    /// <summary>
    /// Step through lines and run any mutations until we either hit some dialogue or the end of the conversation.
    /// </summary>
    public static Task<DialogueLineWrapper?> GetNextDialogueLine(Resource resource, string key)
    {
        DialogueManagerWrapper._getNextDialogueLineParameters[0] = resource;
        DialogueManagerWrapper._getNextDialogueLineParameters[1] = key;
        return ((Task<dynamic?>)DialogueManagerWrapper._getNextDialogueLineMethodInfo
            .Invoke(null, DialogueManagerWrapper._getNextDialogueLineParameters)!)
            .ContinueWith(DialogueManagerWrapper.ToDialogueLineWrapper);
    }

    /// <summary>
    /// // TODO: document this.
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
    }

    /// <summary>
    /// // TODO: document this.
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