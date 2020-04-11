#version 430 core
#extension GL_ARB_gpu_shader_fp64 : enable

in vec4 gl_FragCoord;
uniform vec2 complexLocation;
uniform vec2 windowSize;
uniform float zoom;
uniform bool mandelbrot;
out vec4 fragColor;

int mPix(){
    int i = 0;
    vec2 uv = gl_FragCoord.xy;

    float scaledX = (float(uv.x) - float(windowSize.x) / 2) / (float(windowSize.x) / 2) / zoom + complexLocation.x;
    float scaledY = (float(uv.y) - float(windowSize.y) / 2) / (float(windowSize.y) / 2) / (zoom * (windowSize.x / windowSize.y)) + complexLocation.y;

    float x = float(0);
    float y = float(0);

    while (x * x + y * y < float(4) && i < 256)
    {
        float xTemp = x * x - y * y + scaledX;
        y = 2 * x * y + scaledY;
        x = xTemp;

        i++;
    }

    return i;
}

int jPix(){
    int i = 0;
    vec2 uv = gl_FragCoord.xy;

    float zx = (float(uv.x) - float(windowSize.x) / 2) / (float(windowSize.x) / 2) / zoom;
    float zy = (float(uv.y) - float(windowSize.y) / 2) / (float(windowSize.y) / 2) / (zoom * (windowSize.x / windowSize.y));

    while (zx * zx + zy * zy < float(4) && i < 256)
    {
        float xTemp = zx * zx - zy * zy;
        zy = 2 * zx * zy + complexLocation.y;
        zx = xTemp + complexLocation.x;

        i++;
    }

    return i;
}

void main()
{    
    int i;
    if (mandelbrot)
        i = mPix();
    else i = jPix();

    if (i > 255)
        fragColor = vec4(0, 0, 0, 1.0);
    else
    {
        float value = sqrt(float(i) / 256);
        fragColor = vec4(.2 * value, .2 * value, value, 1);
    }
}