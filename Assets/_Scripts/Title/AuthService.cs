using Firebase.Auth;
using Google;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class AuthService : MonoBehaviour
{
    public FirebaseAuth Auth { get; private set; }

    // 현재 로그인된 사용자 정보
    public FirebaseUser CurrentUser => Auth.CurrentUser;

    // 로그인 상태 확인
    public bool IsLoggedIn => Auth.CurrentUser != null;

    // Google Web Client Id 값
    private string webClientId = "817505350827-emeqqf39h3946aqf1kjo70vi2hm8ih9j.apps.googleusercontent.com";
    private bool isSigningIn = false;

    public void Initialize()
    {
        Auth = FirebaseAuth.DefaultInstance;
        Debug.Log("[Auth] Firebase Auth initialized");

        GoogleSignInConfiguration config = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestIdToken = true
        };
        GoogleSignIn.Configuration = config;

        Debug.Log("컨피그 완료");
    }

    public async Task<FirebaseUser> SignInWithGoogleAsync()
    {
        if (isSigningIn) return null;
        isSigningIn = true;

        try
        {
            Debug.Log("1. 구글 API 호출 시작");

            // 에디터/기기 환경에 따른 태스크 처리
            var signInTask = GoogleSignIn.DefaultInstance.SignIn();
            GoogleSignInUser googleUser = await signInTask;

            if (googleUser == null)
            {
                Debug.LogError("2. 구글 사용자 정보가 NULL입니다 (취소 또는 에러)");
                return null;
            }

            Debug.Log($"3. 구글 인증 성공: {googleUser.DisplayName}");

            // 토큰 확인
            if (string.IsNullOrEmpty(googleUser.IdToken))
            {
                Debug.LogError("4. IdToken이 없습니다. 설정을 확인하세요.");
                return null;
            }

            Debug.Log("5. 구글 토큰 생성 시작");
            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

            Debug.Log("6. 파이어베이스 자격 증명 로그인 시도");
            var authResult = await Auth.SignInWithCredentialAsync(credential);

            return authResult;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Critical Error] 로그인 중 예외 발생: {ex}");
            return null;
        }
        finally
        {
            isSigningIn = false;
            Debug.Log("8. 로그인 시도 상태 해제");
        }
    }

    // 비동기 로그인
    public async Task<FirebaseUser> LoginAsync(string email, string password)
    {
        var authResult = await Auth.SignInWithEmailAndPasswordAsync(email, password);
        return authResult.User;
    }

    // 비동기 회원가입
    public async Task<FirebaseUser> SignUpAsync(string email, string password)
    {
        var authResult = await Auth.CreateUserWithEmailAndPasswordAsync(email, password);
        return authResult.User;
    }

    // 사용자 프로필 업데이트 (닉네임 설정)
    public async Task UpdateProfileAsync(FirebaseUser user, string displayName)
    {
        var profile = new UserProfile { DisplayName = displayName };
        await user.UpdateUserProfileAsync(profile);
    }

    // 로그아웃
    public void Logout()
    {
        Auth.SignOut();
        if(GoogleSignIn.DefaultInstance != null)
        {
            GoogleSignIn.DefaultInstance.SignOut();
        }
        Debug.Log("[Auth] User signed out");
    }
}
