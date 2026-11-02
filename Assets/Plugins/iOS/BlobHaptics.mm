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
