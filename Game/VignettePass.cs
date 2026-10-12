using Age.Core;
using Age.Rendering;

namespace Age.Platformer;

/// <summary>
/// Draws the vignette of the demo over the frame, which is what a post-process of a game looks like: a quad covers the frame in
/// pixels, and the fragment stage of the content reads the surface the quad is drawn into - the world, the text and the interface as
/// they stand - and writes it back with its edges darkened.
/// </summary>
/// <remarks>
/// It runs after the world and the interface and before the overlay of the developer, so the console and the numbers of a frame stay
/// above it. A pass of a game is added to the same pipeline as the passes of the engine, which is the whole of what a pipeline is for.
/// </remarks>
internal sealed class VignettePass(IRenderer renderer, ShaderHandle vignette) : IRenderPass
{
    /// <summary>Whether the pass draws the vignette, which the command `vignette` turns off and on while the game runs.</summary>
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public void Render(World world, in Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!Enabled)
        {
            return;
        }

        // A camera that maps one unit to one pixel of the frame: what this pass covers is the frame rather than the world.
        var screenCamera = new Camera2D { Position = Vector2.Zero, Zoom = 1f, ViewportSize = renderer.ViewportSize };

        // The engine copies the surface into a texture when a shader declares the sampler of it, which is what this call does for the
        // stage below: what it reads is the frame as it stands when this pass runs, so the vignette is a picture of the frame with its
        // edges darkened rather than a colour over it. The two settings below are those of the demo, not of the engine.
        renderer.SetCamera(screenCamera);
        renderer.BeginFrame(false);
        renderer.UseShader(vignette);
        renderer.SetUniform("uVignetteColour", [0.16f, 0.20f, 0.38f]);
        renderer.SetUniform("uVignetteStrength", 0.85f);
        renderer.DrawRectangle(new Rect(Vector2.Zero, renderer.ViewportSize), Color.White);
        renderer.ResetShader();
        renderer.EndFrame();
    }
}

