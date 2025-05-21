const static float PI = 3.14159;

// original ver.
float SmoothingKernelPoly6(float r, float h) {
    if (r > h) return 0.0;
    float scale = 315 / (64 * PI * pow(abs(h), 9));
    float v = h * h - r * r;
    return v * v * v * scale; 
}

// paper ver.
float VisocityKernel(float r, float h) {
    if (r > h) return 0.0;
    float scale = 15 / (2 * PI * pow(h, 3));
    float v = -pow(r, 3) / (2 * h * h * h) + pow(r, 2) / (h * h) + h / (2*r) - 1;
    return v * scale;
}

float SpikyKernelPow3(float r, float h) {
    if (r > h) return 0.0;
    float scale = 15 / (PI * pow(h, 6));
    float v = h - r;
    return v * v * v * scale;
}

float DerivativeSpikyKernelPow3(float r, float h) {
    if (r > h) return 0.0;
    float scale = 45 / (PI * pow(h, 6));
    float v = h - r;
    return v * v * scale;
}

float SpikyKernelPow2(float r, float h) {
    if (r > h) return 0.0;
    float scale = 15 / (PI * pow(h, 5));
    float v = h - r;
    return v * v * scale;
}

float DerivativeSpikyKernelPow2(float r, float h) {
    if (r > h) return 0.0;
    float scale = 30 / (PI * pow(h, 5));
    float v = h - r;
    return v * scale;
}



