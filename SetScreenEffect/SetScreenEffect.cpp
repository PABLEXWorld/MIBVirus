#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <magnification.h>

BOOL __stdcall _start(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    return TRUE;
}
float mul11, mul12, mul13,
mul21, mul22, mul23,
mul31, mul32, mul33;
float fade = (float)1;

void SetScreenEffect(
	float mul11new, float mul12new, float mul13new,
	float mul21new, float mul22new, float mul23new,
	float mul31new, float mul32new, float mul33new) {
	mul11 = mul11new;
	mul12 = mul12new;
	mul13 = mul13new;
	mul21 = mul21new;
	mul22 = mul22new;
	mul23 = mul23new;
	mul31 = mul31new;
	mul32 = mul32new;
	mul33 = mul33new;
	MAGCOLOREFFECT magColorEffect = {
		fade * mul11, fade * mul12, fade * mul13, (float)0, (float)0,
		fade * mul21, fade * mul22, fade * mul23, (float)0, (float)0,
		fade * mul31, fade * mul32, fade * mul33, (float)0, (float)0,
		(float)0, (float)0, (float)0, (float)1, (float)0,
		(float)0, (float)0, (float)0, (float)0, (float)1 };
	MagSetFullscreenColorEffect(&magColorEffect);
}

void SetScreenRot(int grados) {
	int fase = grados / 120;
	float numero1 = grados % 120 / (float)120;
	float mul11new, mul12new, mul13new,
		mul21new, mul22new, mul23new,
		mul31new, mul32new, mul33new;
	switch (fase)
	{
	case 0:
		mul11new = (float)1 - numero1;
		mul12new = numero1;
		mul13new = (float)0;
		mul21new = (float)0;
		mul22new = (float)1 - numero1;
		mul23new = numero1;
		mul31new = numero1;
		mul32new = (float)0;
		mul33new = (float)1 - numero1;
		break;
	case 1:
		mul11new = (float)0;
		mul12new = (float)1 - numero1;
		mul13new = numero1;
		mul21new = numero1;
		mul22new = (float)0;
		mul23new = (float)1 - numero1;
		mul31new = (float)1 - numero1;
		mul32new = numero1;
		mul33new = (float)0;
		break;
	case 2:
		mul11new = numero1;
		mul12new = (float)0;
		mul13new = (float)1 - numero1;
		mul21new = (float)1 - numero1;
		mul22new = numero1;
		mul23new = (float)0;
		mul31new = (float)0;
		mul32new = (float)1 - numero1;
		mul33new = numero1;
		break;
	}
	SetScreenEffect(
		mul11new, mul12new, mul13new,
		mul21new, mul22new, mul23new,
		mul31new, mul32new, mul33new);
}

void SetScreenFade(float newfade) {
	fade = newfade;
	MAGCOLOREFFECT magColorEffect = {
		fade * mul11, fade * mul12, fade * mul13, (float)0, (float)0,
		fade * mul21, fade * mul22, fade * mul23, (float)0, (float)0,
		fade * mul31, fade * mul32, fade * mul33, (float)0, (float)0,
		(float)0, (float)0, (float)0, (float)1, (float)0,
		(float)0, (float)0, (float)0, (float)0, (float)1 };
	MagSetFullscreenColorEffect(&magColorEffect);
}
