#version 450

layout(location = 0) in vec2 fragPos;
layout(location = 1) in vec4 fragColor;

layout(location = 0) out vec4 outColor;

layout(push_constant) uniform Push
{
    vec2 center;
    float radius;
} pc;

void main()
{
    float dist = length(fragPos - pc.center);

    if (dist > pc.radius)
        discard;

    outColor = fragColor;
}