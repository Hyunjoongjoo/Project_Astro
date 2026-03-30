using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#region Firestore Datas
[FirestoreData]
public class DbModel
{
    [FirestoreProperty("Profile")] public ProfileDbModel profile { get; set; }

    [FirestoreProperty("Record")] public RecordModel record { get; set; }

    [FirestoreProperty("Wallet")] public WalletModel wallet { get; set; }
}

[FirestoreData]
public class ProfileDbModel
{
    [FirestoreProperty] public string uuid { get; set; }
    [FirestoreProperty] public string nickName { get; set; }
    [FirestoreProperty] public int userLevel { get; set; }
    [FirestoreProperty] public int userExp { get; set; }
    [FirestoreProperty] public bool isAgreed { get; set; }
    [FirestoreProperty] public Timestamp createdAt { get; set; }
}

[FirestoreData]
public class HeroDbModel
{
    [FirestoreProperty] public string heroId { get; set; }
    [FirestoreProperty] public int level { get; set; }
    [FirestoreProperty] public int exp { get; set; }
    [FirestoreProperty] public bool isUnlock { get; set; }
}

[FirestoreData]
public class RecordModel
{
    [FirestoreProperty] public int win { set; get; }
    [FirestoreProperty] public int draw { set; get; }
    [FirestoreProperty] public int lose { set; get; }
}

[FirestoreData]
public class WalletModel
{
    [FirestoreProperty] public int gold { set; get; }
}

#endregion

// Firestore 유저 데이터

public class UserDataStore : Singleton<UserDataStore>
{
    private FirebaseFirestore _firestore;
    private const string COLLECTION_NAME = "users";
    private const string COLLECTION_SESSION = "user_sessions";
    private const string COLLECTION_HERO = "COL_Hero";

    public void Initialize()
    {
        _firestore = FirebaseFirestore.DefaultInstance;
        _firestore.Settings.PersistenceEnabled = false;
    }

    #region DB Create / Delete

    // 새 유저 데이터 생성
    public async Task CreateUserDataAsync(string uuid, string nickname)
    {
        DocumentReference userDocRef = _firestore.Collection(COLLECTION_NAME).Document(uuid);
        DocumentReference sessionDocRef = _firestore.Collection(COLLECTION_SESSION).Document(uuid);
        Debug.Log($"[Step 1] CreateUserDataAsync 진입 - UUID: {uuid}");

        try
        {
            WriteBatch batch = _firestore.StartBatch();
            Debug.Log("[Step 2] 배치 생성 완료");

            // 0. Hero제외 다른 데이터의 묶음 처리
            var data = new Dictionary<string, object>
            {
                // Profile Map 생성
                { "Profile", new Dictionary<string, object>
                    {
                        { "uuid", uuid },
                        { "nickName", nickname },
                        { "userLevel", 1 },
                        { "userExp", 0 },
                        { "isAgreed", true },
                        { "createdAt", FieldValue.ServerTimestamp }
                    }
                },
                // Record Map 생성
                { "Record", new Dictionary<string, object>
                    {
                        { "win", 0 },
                        { "draw", 0 },
                        { "lose", 0 }
                    }
                },
                // Wallet Map 생성
                { "Wallet", new Dictionary<string, object>
                    {
                        { "gold", 0 }
                    }
                }
            };
            batch.Set(userDocRef, data);
            Debug.Log("유저데이터 배치 완료");

            var sessionData = new Dictionary<string, object>
            {
                { "sessionId", "" },
                { "lastLoginAt", FieldValue.ServerTimestamp }
            };
            batch.Set(sessionDocRef, sessionData);

            if (TableManager.Instance == null)
            {
                Debug.LogError("TableManager 인스턴스가 없습니다!");
                return;
            }

            if (TableManager.Instance.HeroTable == null)
            {
                Debug.LogError("HeroTable이 로드되지 않았습니다!");
                return;
            }

            // 3. 영웅 정보 생성 (CSV파서를 통한 TableManager에서 Id값 가져옴)
            var csvHeroDatas = TableManager.Instance.HeroTable.GetAll();

            Debug.Log($"[Step 4] CSV 영웅 데이터 가져옴: {csvHeroDatas?.Count ?? 0}개");

            if (csvHeroDatas != null && csvHeroDatas.Count > 0)
            {
                foreach (var heroData in csvHeroDatas)
                {
                    string heroId = heroData.PrimaryID;

                    var initHeroDbData = new Dictionary<string, object>
                    {
                        { "isUnlock", false },
                        { "level", 1 },
                        { "exp", 0 }
                    };

                    // 서브 컬렉션에 영웅들 정보 생성
                    batch.Set(userDocRef.Collection(COLLECTION_HERO).Document(heroId), initHeroDbData);
                }
            }
            Debug.Log("[Step 5] 영웅 데이터 배치 설정 완료");
            // awite Exception 관련 이슈 출력 기능 추가.
            await batch.CommitAsync();
            Debug.Log($"[Firestore] 유저 '{nickname}' 생성 및 기본 영웅 {csvHeroDatas.Count}종 생성 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firestore] 초기화 실패: {e.Message}");
        }
    }

    // 기존 DB에 새 영웅 데이터 생성
    public async Task AddNewHeroAsync(string uuid, string heroId)
    {
        var heroDocRef = _firestore.Collection(COLLECTION_NAME).Document(uuid)
            .Collection(COLLECTION_HERO).Document(heroId);

        var initHeroData = new Dictionary<string, object>
        {
            { "isUnlock", false },
            { "level", 1 },
            { "exp", 0 }
        };

        await heroDocRef.SetAsync(initHeroData);
        Debug.Log($"[Firestore] 신규 영웅 {heroId} 추가 완료");
    }

    // 기존 DB에 사라진 영웅 데이터 삭제
    public async Task DeleteHeroAsync(string uuid, string heroId)
    {
        try
        {
            await _firestore.Collection(COLLECTION_NAME).Document(uuid)
                .Collection(COLLECTION_HERO).Document(heroId)
                .DeleteAsync();

            Debug.Log($"[Firestore] 영웅 {heroId} 삭제 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Firestore] 영웅 삭제 실패: {e.Message}");
        }
    }

    // 계정 삭제
    public async Task DeleteUserId(string uuid)
    {
        try
        {
            WriteBatch batch = _firestore.StartBatch();
            DocumentReference userDoc = _firestore.Collection(COLLECTION_NAME).Document(uuid);
            DocumentReference sessionDoc = _firestore.Collection(COLLECTION_SESSION).Document(uuid);

            // 서브 컬렉션 모든 문서 조회하기
            QuerySnapshot heroSnapshot = await userDoc.Collection("COL_Hero").GetSnapshotAsync();
            foreach (DocumentSnapshot doc in heroSnapshot.Documents)
            {
                batch.Delete(doc.Reference);
            }

            // 최상위 컬렉션 삭제 배치
            batch.Delete(userDoc);
            batch.Delete(sessionDoc);

            // DB 실행.
            await batch.CommitAsync();

            Debug.Log($"[Firestore] 유저({uuid})의 모든 데이터(영웅 포함) 삭제 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Firestore] 데이터 완전 삭제 실패: {e.Message}");
        }
    }

    #endregion

    #region DB Search
    // 유저 데이터 조회(uuid, 닉네임,경험치, 레벨, 생성 날짜 정보 사용)
    public async Task<DbModel> GetUserDataAsync(string uuid)
    {
        var snapshot = await _firestore
            .Collection(COLLECTION_NAME)
            .Document(uuid)
            .GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            Debug.LogWarning($"[Firestore] User data not found for UUID: {uuid}");
            return null;
        }

        var userData = snapshot.ConvertTo<DbModel>();
        return userData;
    }

    // 영웅 정보 조회(로그인 시, 게임 종료 시 사용)
    public async Task<List<HeroDbModel>> GetUserHeroDataAsync(string uuid)
    {
        List<HeroDbModel> heroList = new List<HeroDbModel>();

        // 경로: users/{uuid}/Hero (서브컬렉션)
        QuerySnapshot heroQuerySnapshot = await _firestore
            .Collection(COLLECTION_NAME)
            .Document(uuid)
            .Collection(COLLECTION_HERO)
            .GetSnapshotAsync();

        if (heroQuerySnapshot.Count == 0)
        {
            Debug.LogWarning($"[Firestore] 유저 {uuid}에게 등록된 영웅 데이터가 없습니다.");
            return heroList;
        }

        foreach (DocumentSnapshot heroDoc in heroQuerySnapshot.Documents)
        {
            if (heroDoc.Exists)
            {
                HeroDbModel heroData = heroDoc.ConvertTo<HeroDbModel>();
                heroData.heroId = heroDoc.Id;
                heroList.Add(heroData);
            }
        }

        Debug.Log($"[Firestore] 유저 {uuid}로부터 {heroList.Count}개의 영웅 정보를 로드했습니다.");
        return heroList;
    }
    #endregion

    #region DB Update
    // 유저 데이터 모두 업데이트
    public async Task UpdateAllAsync(string uuid, Dictionary<string, object> updates = null, List<HeroDbModel> heroDatas = null)
    {
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
        {
            try
            {
                WriteBatch batch = _firestore.StartBatch();

                if (updates != null)
                {
                    DocumentReference userDocRef = _firestore.Collection(COLLECTION_NAME).Document(uuid);
                    batch.Update(userDocRef, updates);
                }

                if (heroDatas != null)
                {
                    foreach (var hero in heroDatas)
                    {
                        var heroDocRef = _firestore.Collection(COLLECTION_NAME).Document(uuid)
                                            .Collection(COLLECTION_HERO).Document(hero.heroId);

                        Dictionary<string, object> heroUpdates = new Dictionary<string, object>
                    {
                        { "level", hero.level },
                        { "exp", hero.exp },
                        { "isUnlock", hero.isUnlock }
                    };

                        batch.Update(heroDocRef, heroUpdates);
                        Debug.Log($"[Batch Queue] 영웅 {hero.heroId} (Level: {hero.level}) 추가");
                    }
                }

                Task commitTask = batch.CommitAsync();
                Task delayTask = Task.Delay(TimeSpan.FromSeconds(20), cts.Token);
                Task completedTask = await Task.WhenAny(commitTask, delayTask);

                if (completedTask == delayTask)
                {
                    throw new TimeoutException("[Firestore] 서버 응답 시간이 초과되었습니다. (20s)");
                }
                await commitTask;
                Debug.Log("[Batch] DB UpdateAll 성공");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firestore] All Update 실패: {e.Message}");
            }
        }
    }

    // 세션 업데이트
    public async Task UpdateSessionIdAsync(string uuid, string sessionId)
    {
        try
        {
            DocumentReference sessionDocRef = _firestore.Collection(COLLECTION_SESSION).Document(uuid);
            Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "sessionId", sessionId }, 
        };
            await sessionDocRef.SetAsync(updates, SetOptions.MergeAll);
            Debug.Log($"[Firestore] 세션 업데이트 성공: {sessionId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firestore] 세션 업데이트 실패: {e.Message}");
        }
    }

    #endregion


    // 닉네임 중복 체크
    public async Task<bool> IsNicknameDuplicateAsync(string nickname)
    {
        var query = await _firestore.
            Collection(COLLECTION_NAME).
            WhereEqualTo("Profile.nickName", nickname)
            .GetSnapshotAsync();

        return query.Count > 0;
    }
}
