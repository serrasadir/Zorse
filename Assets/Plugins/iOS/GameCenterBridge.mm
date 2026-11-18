// Game Center ekranları (Unity'nin Social API'sinde yok): profil (ad + fotoğraf), arkadaş ekleme, panel.
// C# tarafı: Assets/_Project/Scripts/Systems/Social/SocialService.cs (GameCenterService).
#import <GameKit/GameKit.h>
#import <UIKit/UIKit.h>

extern "C" UIViewController* UnityGetGLViewController();

@interface LNCGameCenterDelegate : NSObject <GKGameCenterControllerDelegate>
@end

@implementation LNCGameCenterDelegate
- (void)gameCenterViewControllerDidFinish:(GKGameCenterViewController *)controller
{
    [controller dismissViewControllerAnimated:YES completion:nil];
}
@end

static LNCGameCenterDelegate *s_delegate = nil;

static void LNC_Present(GKGameCenterViewControllerState state)
{
    if (!GKLocalPlayer.localPlayer.isAuthenticated) return;
    GKGameCenterViewController *controller = [[GKGameCenterViewController alloc] initWithState:state];
    if (s_delegate == nil) s_delegate = [LNCGameCenterDelegate new];
    controller.gameCenterDelegate = s_delegate;
    [UnityGetGLViewController() presentViewController:controller animated:YES completion:nil];
}

extern "C" void LNC_ShowProfile()
{
    LNC_Present(GKGameCenterViewControllerStateLocalPlayerProfile);
}

extern "C" void LNC_ShowDashboard()
{
    LNC_Present(GKGameCenterViewControllerStateDashboard);
}

// iOS 15+ (projenin alt sınırı): arkadaşlık isteği gönderme ekranı. Açılamazsa Game Center paneli.
extern "C" void LNC_AddFriends()
{
    if (!GKLocalPlayer.localPlayer.isAuthenticated) return;
    NSError *error = nil;
    [GKLocalPlayer.localPlayer presentFriendRequestCreatorFromViewController:UnityGetGLViewController() error:&error];
    if (error != nil) LNC_Present(GKGameCenterViewControllerStateDashboard);
}
