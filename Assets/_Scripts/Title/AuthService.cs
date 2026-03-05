using Firebase.Auth;
using System.Threading.Tasks;
using UnityEngine;

public class AuthService : MonoBehaviour
{
    public FirebaseAuth Auth { get; private set; }

    // 현재 로그인된 사용자 정보
    public FirebaseUser CurrentUser => Auth.CurrentUser;

    // 로그인 상태 확인
    public bool IsLoggedIn => Auth.CurrentUser != null;

    public void Initialize()
    {
        Auth = FirebaseAuth.DefaultInstance;
        Debug.Log("[Auth] Firebase Auth initialized");
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
        Debug.Log("[Auth] User signed out");
    }
}
