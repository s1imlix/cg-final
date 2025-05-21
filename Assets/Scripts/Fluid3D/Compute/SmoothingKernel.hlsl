const static float PI = 3.14159;

float smoothingKernel(float distance, float h) {
    if (distance > h) return 0.0;
    float scale = 15 / (2 * PI * pow(h, 5));
	float v = h - distance;
	return v * v * scale; //TBD
}

float smoothingKernelDerivative(float distance, float h) {
    if (distance > h) return 0.0;
    float scale = 15 / (pow(h, 5) * 3.14159);
	float v = h - distance;
	return -v * scale; //TBD
}

float SmoothingKernelPoly6(float distance, float h) {
    if (distance > h) return 0.0;
    float scale = 315 / (64 * PI * pow(abs(h), 9));
    float v = h * h - distance * distance;
    return v * v * v * scale; // TBD
}