namespace NTComponents;

/// <summary>
///     Specifies the available animation types for the <see cref="NTAnimation" /> component.
/// </summary>
public enum Animation {

    /// <summary>
    ///     Fades the element in.
    /// </summary>
    FadeIn,

    /// <summary>
    ///     Fades the element in and slides it from the top.
    /// </summary>
    SlideTop,

    /// <summary>
    ///     Fades the element in and slides it from the bottom.
    /// </summary>
    SlideBottom,

    /// <summary>
    ///     Fades the element in and slides it from the left.
    /// </summary>
    SlideLeft,

    /// <summary>
    ///     Fades the element in and slides it from the right.
    /// </summary>
    SlideRight,

    /// <summary>
    ///     Flips the element horizontally.
    /// </summary>
    Flip,

    /// <summary>
    ///     Rotates the object 90 degrees clockwise.
    /// </summary>
    RotateClockwise,

    /// <summary>
    ///     Rotates the object 90 degrees counterclockwise.
    /// </summary>
    RotateCounterClockwise,
}
