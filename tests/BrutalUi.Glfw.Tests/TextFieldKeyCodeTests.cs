using Brutal.GlfwApi;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Glfw.Tests;

/// <summary>
/// Tests of the key codes of <see cref="TextField"/>. The internal Core values must equal the <see cref="GlfwKey"/> values
/// of the binding, because the adapter passes the GLFW key code through with no map.
/// </summary>
public sealed class TextFieldKeyCodeTests
{
    /// <summary>
    /// The edit key codes of <c>WidgetInput</c> equal the <see cref="GlfwKey"/> values: Backspace, Delete, Home, End, the
    /// main Enter key, Left, and Right.
    /// </summary>
    [Fact]
    public void TheTextFieldKeyCodesEqualTheGlfwKeys()
    {
        Assert.Equal(WidgetInput.BackspaceKey, (int)GlfwKey.Backspace);
        Assert.Equal(WidgetInput.DeleteKey, (int)GlfwKey.Delete);
        Assert.Equal(WidgetInput.HomeKey, (int)GlfwKey.Home);
        Assert.Equal(WidgetInput.EndKey, (int)GlfwKey.End);
        Assert.Equal(WidgetInput.EnterKey, (int)GlfwKey.Enter);
        Assert.Equal(WidgetInput.LeftKey, (int)GlfwKey.Left);
        Assert.Equal(WidgetInput.RightKey, (int)GlfwKey.Right);
    }
}
