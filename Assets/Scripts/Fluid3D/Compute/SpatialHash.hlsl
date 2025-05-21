static const int3 inRadCellOffset[27] = {
    int3(-1, -1, -1), int3(0, -1, -1), int3(1, -1, -1),
    int3(-1, 0, -1), int3(0, 0, -1), int3(1, 0, -1),
    int3(-1, 1, -1), int3(0, 1, -1), int3(1, 1, -1),
    int3(-1, -1, 0), int3(0, -1, 0), int3(1, -1, 0),
    int3(-1, 0, 0), int3(0, 0, 0), int3(1, 0, 0),
    int3(-1, 1, 0), int3(0, 1, 0), int3(1, 1, 0),
    int3(-1, -1, 1), int3(0, -1, 1), int3(1, -1, 1),
    int3(-1, 0, 1), int3(0, 0, 1), int3(1, 0, 1),
    int3(-1, 1, 1), int3(0, 1, 1), int3(1, 1, 1)
};

// Constants used for hashing
static const uint hashK1 = 73856093;
static const uint hashK2 = 19349663;
static const uint hashK3 = 83492791;

// Convert floating point position into an integer cell coordinate
int3 PositionToCell3D(float3 position, float radius)
{
	return (int3)floor(position / radius);
}

// Hash cell coordinate to a single unsigned integer
uint HashCell3D(int3 cell)
{
	cell = (uint3) cell;
	return (cell.x * hashK1) + (cell.y * hashK2) + (cell.z * hashK3);
}

uint KeyFromHash(uint hash, uint tableSize)
{
	return hash % tableSize;
}



/*
static const uint hashK1 = 15823;
static const uint hashK2 = 9737333;
static const uint hashK3 = 440817757;

int3 PositionToCell3D(float3 point, float radius)
{
    return (int3)floor(point / radius); // we want the ones within one cell
}

uint HashCell3D(int3 cellCoord)
{
    cellCoord = (uint3)cellCoord;
    return (cellCoord.x * hashK1) + (cellCoord.y * hashK2) + (cellCoord.z * hashK3);
}

uint KeyFromHash(uint hash, uint lookupTableSize)
{
    return hash % lookupTableSize;
}
*/