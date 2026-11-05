// 2026-11-02: Yeme titreşimi için hafif iOS dokunuşu (bkz. Assets/_Project/Scripts/Core/Haptics.cs).
// Handheld.Vibrate() bildirim titreşimi kadar güçlüydü; Light impact klavye tıklaması kadar hafif.
#import <UIKit/UIKit.h>

static UIImpactFeedbackGenerator *s_lightGenerator = nil;

extern "C" void BlobHaptics_LightImpact()
{
    if (s_lightGenerator == nil)
        s_lightGenerator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];

    [s_lightGenerator impactOccurred];
    [s_lightGenerator prepare]; // bir sonraki dokunuş gecikmesiz gelsin
}

// 1 = Medium, 2 = Heavy (büyük lokmalar). Light için BlobHaptics_LightImpact.
static UIImpactFeedbackGenerator *s_mediumGenerator = nil;
static UIImpactFeedbackGenerator *s_heavyGenerator = nil;

extern "C" void BlobHaptics_Impact(int style)
{
    if (style >= 2)
    {
        if (s_heavyGenerator == nil)
            s_heavyGenerator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
        [s_heavyGenerator impactOccurred];
        [s_heavyGenerator prepare];
        return;
    }
    if (s_mediumGenerator == nil)
        s_mediumGenerator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
    [s_mediumGenerator impactOccurred];
    [s_mediumGenerator prepare];
}
