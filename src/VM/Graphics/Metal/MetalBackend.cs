using JSONScript.VM.Graphics;
using JSONScript.Runtime;

namespace JSONScript.VM.Graphics.Metal
{
    public class MetalBackend : IGraphicsBackend
    {
        private AppKitWindow? window;
        private MetalRenderer? renderer;

        public void Init(int width, int height, string title)
        {
            window = new AppKitWindow(width, height, title);
            window.HandleWindowClose();
            renderer = window.InitMetal(width, height);
        }

        public void SetEventHandler(Action<string, Value[]> handler)
        {
            window!.OnEvent = handler;
        }

        public void DrawRect(float x, float y, float w, float h, float r, float g, float b) => renderer!.DrawRect(x, y, w, h, r, g, b);
        public void DrawTriangle(float x1, float y1, float x2, float y2, float x3, float y3, float r, float g, float b) => renderer!.DrawRect(x1, y1, x2, y2, r, g, b);
        public void DrawCircle(float x, float y, float radius, float r, float g, float b) => renderer!.DrawRect(x, x, x, x, r, g, b);

        public void BeginFrame() { }
        public void EndFrame() { }

        public void RunLoop() => window!.RunLoop();

        public nint GetLayerPtr()  => window!.Renderer!.LayerPtr;
        public nint GetDevicePtr() => window!.Renderer!.DevicePtr;
    }
}