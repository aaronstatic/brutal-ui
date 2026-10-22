namespace BrutalUi.Core.Tests;

/// <summary>One handler call that an <see cref="EventRecorder"/> recorded.</summary>
internal struct EventRecord
{
    /// <summary>The current target of the event at the time of the call.</summary>
    public Element? CurrentTarget;

    /// <summary>The phase of the walk at the time of the call.</summary>
    public UiEventPhase Phase;

    /// <summary>The type of the event.</summary>
    public UiEventType Type;

    /// <summary><see langword="true"/> when the walk called a capture slot. <see langword="false"/> for a bubble slot.</summary>
    public bool CaptureSlot;
}

/// <summary>
/// Records the handler calls of the event walk in a fixed array.
/// </summary>
/// <remarks>
/// The recorder creates its array and its delegates in the constructor. A record allocates nothing. A full array
/// stops the record and sets <see cref="Overflow"/>.
/// </remarks>
internal sealed class EventRecorder
{
    /// <summary>The number of records that the recorder holds at most.</summary>
    public const int Capacity = 256;

    private readonly EventRecord[] _records = new EventRecord[Capacity];
    private Element? _stopElement;
    private UiEventPhase _stopPhase;
    private bool _stopCaptureSlot;

    /// <summary>Creates a recorder with no record, and creates its pointer delegates one time.</summary>
    public EventRecorder()
    {
        PointerCapture = RecordPointerCapture;
        PointerBubble = RecordPointerBubble;
        ScrollCapture = RecordScrollCapture;
        ScrollBubble = RecordScrollBubble;
        KeyCapture = RecordKeyCapture;
        KeyBubble = RecordKeyBubble;
        CharCapture = RecordCharCapture;
        CharBubble = RecordCharBubble;
    }

    /// <summary>The copy of the last pointer event that a call recorded in the target phase.</summary>
    public PointerEvent LastPointer;

    /// <summary>The copy of the last scroll event that a call recorded in the target phase.</summary>
    public ScrollEvent LastScroll;

    /// <summary>The copy of the last key event that a call recorded in the target phase.</summary>
    public KeyEvent LastKey;

    /// <summary>The copy of the last char event that a call recorded in the target phase.</summary>
    public CharEvent LastChar;

    /// <summary>Gets the delegate that records a pointer event as a capture slot call.</summary>
    public UiEventHandler<PointerEvent> PointerCapture { get; }

    /// <summary>Gets the delegate that records a pointer event as a bubble slot call.</summary>
    public UiEventHandler<PointerEvent> PointerBubble { get; }

    /// <summary>Gets the delegate that records a scroll event as a capture slot call.</summary>
    public UiEventHandler<ScrollEvent> ScrollCapture { get; }

    /// <summary>Gets the delegate that records a scroll event as a bubble slot call.</summary>
    public UiEventHandler<ScrollEvent> ScrollBubble { get; }

    /// <summary>Gets the delegate that records a key event as a capture slot call.</summary>
    public UiEventHandler<KeyEvent> KeyCapture { get; }

    /// <summary>Gets the delegate that records a key event as a bubble slot call.</summary>
    public UiEventHandler<KeyEvent> KeyBubble { get; }

    /// <summary>Gets the delegate that records a char event as a capture slot call.</summary>
    public UiEventHandler<CharEvent> CharCapture { get; }

    /// <summary>Gets the delegate that records a char event as a bubble slot call.</summary>
    public UiEventHandler<CharEvent> CharBubble { get; }

    /// <summary>Gets the number of records.</summary>
    public int Count { get; private set; }

    /// <summary>Gets a value that tells if a call came when the array was full.</summary>
    public bool Overflow { get; private set; }

    /// <summary>Gets a record.</summary>
    /// <param name="index">The index of the record, in the order of the calls.</param>
    public EventRecord this[int index] => _records[index];

    /// <summary>
    /// Sets the call that stops the event. After the recorder records a call with this element, this phase, and this
    /// slot, the recorder calls <see cref="UiEventHeader.StopPropagation"/> on the event. The setting does not
    /// allocate and stays after <see cref="Reset"/>.
    /// </summary>
    /// <param name="element">The element whose call stops the event.</param>
    /// <param name="phase">The phase of the call.</param>
    /// <param name="captureSlot"><see langword="true"/> for a capture slot. <see langword="false"/> for a bubble slot.</param>
    public void StopAt(Element element, UiEventPhase phase, bool captureSlot)
    {
        _stopElement = element;
        _stopPhase = phase;
        _stopCaptureSlot = captureSlot;
    }

    /// <summary>Removes each record and clears <see cref="Overflow"/>.</summary>
    public void Reset()
    {
        System.Array.Clear(_records);
        Count = 0;
        Overflow = false;
    }

    /// <summary>Records a call of a capture slot.</summary>
    /// <typeparam name="T">The event struct.</typeparam>
    /// <param name="e">The event.</param>
    public void Capture<T>(ref T e)
        where T : struct, IUiEvent<T>
    {
        Record(ref T.HeaderOf(ref e), captureSlot: true);
    }

    /// <summary>Records a call of a bubble slot.</summary>
    /// <typeparam name="T">The event struct.</typeparam>
    /// <param name="e">The event.</param>
    public void Bubble<T>(ref T e)
        where T : struct, IUiEvent<T>
    {
        Record(ref T.HeaderOf(ref e), captureSlot: false);
    }

    /// <summary>Sets the 14 event slots of an element to the delegates of this recorder.</summary>
    /// <param name="element">The element.</param>
    public void Register(Element element)
    {
        element.OnPointerMove = PointerBubble;
        element.OnPointerMoveCapture = PointerCapture;
        element.OnPointerDown = PointerBubble;
        element.OnPointerDownCapture = PointerCapture;
        element.OnPointerUp = PointerBubble;
        element.OnPointerUpCapture = PointerCapture;
        element.OnScroll = ScrollBubble;
        element.OnScrollCapture = ScrollCapture;
        element.OnKeyDown = KeyBubble;
        element.OnKeyDownCapture = KeyCapture;
        element.OnKeyUp = KeyBubble;
        element.OnKeyUpCapture = KeyCapture;
        element.OnChar = CharBubble;
        element.OnCharCapture = CharCapture;
    }

    private void RecordPointerCapture(ref PointerEvent e)
    {
        if (e.Header.Phase == UiEventPhase.Target)
        {
            LastPointer = e;
        }

        Capture(ref e);
    }

    private void RecordPointerBubble(ref PointerEvent e)
    {
        if (e.Header.Phase == UiEventPhase.Target)
        {
            LastPointer = e;
        }

        Bubble(ref e);
    }

    private void RecordScrollCapture(ref ScrollEvent e)
    {
        if (e.Header.Phase == UiEventPhase.Target)
        {
            LastScroll = e;
        }

        Capture(ref e);
    }

    private void RecordScrollBubble(ref ScrollEvent e)
    {
        if (e.Header.Phase == UiEventPhase.Target)
        {
            LastScroll = e;
        }

        Bubble(ref e);
    }

    private void RecordKeyCapture(ref KeyEvent e)
    {
        if (e.Header.Phase == UiEventPhase.Target)
        {
            LastKey = e;
        }

        Capture(ref e);
    }

    private void RecordKeyBubble(ref KeyEvent e)
    {
        if (e.Header.Phase == UiEventPhase.Target)
        {
            LastKey = e;
        }

        Bubble(ref e);
    }

    private void RecordCharCapture(ref CharEvent e)
    {
        if (e.Header.Phase == UiEventPhase.Target)
        {
            LastChar = e;
        }

        Capture(ref e);
    }

    private void RecordCharBubble(ref CharEvent e)
    {
        if (e.Header.Phase == UiEventPhase.Target)
        {
            LastChar = e;
        }

        Bubble(ref e);
    }

    private void Record(ref UiEventHeader header, bool captureSlot)
    {
        if (Count == _records.Length)
        {
            Overflow = true;
        }
        else
        {
            _records[Count] = new EventRecord
            {
                CurrentTarget = header.CurrentTarget,
                Phase = header.Phase,
                Type = header.Type,
                CaptureSlot = captureSlot,
            };
            Count++;
        }

        if (_stopElement is not null
            && ReferenceEquals(header.CurrentTarget, _stopElement)
            && header.Phase == _stopPhase
            && captureSlot == _stopCaptureSlot)
        {
            header.StopPropagation();
        }
    }
}
