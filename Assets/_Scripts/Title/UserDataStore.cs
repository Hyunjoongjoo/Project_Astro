using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#region Firestore Datas
[FirestoreData]
public class UserDbModel
{
    [FirestoreProperty] public string uuid { get; set; }
    [FirestoreProperty] public string nickName { get; set; }
    [FirestoreProperty] public int userLevel { get; set; }
    [FirestoreProperty] public int userExp { get; set; }
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
    private const string COLLECTION_PROFILE = "COL_Profile";
    private const string COLLECTION_RECORD = "COL_Record";
    private const string COLLECTION_HERO = "COL_Hero";
    private const string COLLECTION_WALLET = "COL_Wallet";

    private const string DOCUMENT_PROFILE = "DOC_Profile";
    private const string DOCUMENT_RECORD = "DOC_Record";
    private const string DOCUMENT_WALLET = "DOC_Wallet";

    public void Initialize()
    {
        _firestore = FirebaseFirestore.DefaultInstance;
        _firestore.Settings.PersistenceEnabled = false;
        Debug.Log("[Firestore] Firestore initialized");
    }

    #region DB Create / Delete

    // 새 유저 데이터 생성
    public async Task CreateUserDataAsync(string uuid, string nickname)
    {
        DocumentReference userDocRef = _firestore.Collection(COLLECTION_NAME).Document(uuid);

        try
        {
            // 1. 유저 기본 정보 생성
            var profileData = new Dictionary<string, object>
            {
                { "uuid", uuid },
                { "nickName", nickname },
                { "userLevel", 1 },
                { "userExp", 0 },
                { "createdAt", FieldValue.ServerTimestamp }
            };
            await userDocRef.Collection(COLLECTION_PROFILE).Document(DOCUMENT_PROFILE).SetAsync(profileData);

            // 2. 전적 정보 생성
            var Record = new Dictionary<string, object>
            {
                { "win", 0 },
                { "lose", 0 }
            };
            await userDocRef.Collection(COLLECTION_RECORD).Document(DOCUMENT_RECORD).SetAsync(Record);

            // 3. 영웅 정보 생성 (CSV파서를 통한 TableManager에서 Id값 가져옴)
            var csvHeroDatas = TableManager.Instance.HeroTable.GetAll();
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
                    await userDocRef.Collection(COLLECTION_HERO).Document(heroId).SetAsync(initHeroDbData);
                }
            }
            // 4. 지갑(재화) 정보 생성
            var Wallet = new Dictionary<string, object>
            {
                { "gold", 0 }
            };
            await userDocRef.Collection(COLLECTION_WALLET).Document(DOCUMENT_WALLET).SetAsync(Wallet);

            Debug.Log($"[Firestore] 유저 '{nickname}' 생성 및 기본 영웅 {csvHeroDatas.Count}종 생성 완료");
        }
        catch (System.Exception e)
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

    #endregion

    #region DB Search
    // 유저 데이터 조회(uuid, 닉네임,경험치, 레벨, 생성 날짜 정보 사용)
    public async Task<UserDbModel> GetUserDataAsync(string uuid)
    {
        var snapshot = await _firestore
            .Collection(COLLECTION_NAME)
            .Document(uuid)
            .Collection(COLLECTION_PROFILE)
            .Document(DOCUMENT_PROFILE)
            .GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            Debug.LogWarning($"[Firestore] User data not found for UUID: {uuid}");
            return null;
        }

        var userData = snapshot.ConvertTo<UserDbModel>();

        // TODO : UserDataManager에 적용 (싱글톤 패턴 사용 시)
        //if (UserDataManager.Instance != null)
        //{
        //    UserDataManager.Instance.ApplyFromFirestore(
        //        userData.uuid,
        //        userData.nickName,
        //        userData.win,
        //        userData.lose
        //    );
        //}

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

    // 전적 정보 조회(유저 Win, Lose 정보 사용)
    public async Task<RecordModel> GetUserRecordDataAsync(string uuid)
    {
        var snapshot = await _firestore
            .Collection(COLLECTION_NAME)
            .Document(uuid)
            .Collection(COLLECTION_RECORD)
            .Document(DOCUMENT_RECORD)
            .GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            Debug.LogWarning($"[Firestore] User data not found for UUID: {uuid}");
            return null;
        }
        var userData = snapshot.ConvertTo<RecordModel>();

        return userData;
    }

    // 지갑 정보 조회(유저 보유 골드 등)
    public async Task<WalletModel> GetUserWalletDataAsync(string uuid)
    {
        var snapshot = await _firestore
            .Collection(COLLECTION_NAME)
            .Document(uuid)
            .Collection(COLLECTION_WALLET)
            .Document(DOCUMENT_WALLET)
            .GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            Debug.LogWarning($"[Firestore] User data not found for UUID: {uuid}");
            return null;
        }
        var userData = snapshot.ConvertTo<WalletModel>();

        return userData;
    }
    #endregion

    #region DB Update
    // 유저 데이터 업데이트
    public async Task UpdateUserDataAsync(string uuid, Dictionary<string, object> updates)
    {
        await _firestore
            .Collection(COLLECTION_NAME)
            .Document(uuid)
            .Collection(COLLECTION_PROFILE)
            .Document (DOCUMENT_PROFILE)
            .UpdateAsync(updates);

        Debug.Log($"[Firestore] User data updated for UUID: {uuid}");
    }

    // 승패 기록 업데이트
    public async Task UpdateRecordAsync(string uuid, int winDelta, int loseDelta)
    {
        var docRef = _firestore.Collection(COLLECTION_NAME).Document(uuid)
            .Collection(COLLECTION_RECORD).Document(DOCUMENT_RECORD);

        await _firestore.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(docRef);
            var currentWin = snapshot.GetValue<int>("win");
            var currentLose = snapshot.GetValue<int>("lose");

            transaction.Update(docRef, new Dictionary<string, object>
            {
                { "win", currentWin + winDelta },
                { "lose", currentLose + loseDelta }
            });
        });
    }

    // 영웅의 값 업데이트
    public async Task UpdateHeroDataAsync(string uuid, string heroId, int level, int exp, bool isUnlock)
    {
        var heroDocRef = _firestore.Collection(COLLECTION_NAME).Document(uuid)
        .Collection(COLLECTION_HERO).Document(heroId);

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "level", level },
            { "exp", exp },
            { "isUnlock", isUnlock }
        };

        try
        {
            await heroDocRef.UpdateAsync(updates);
    
            Debug.Log($"[Firestore] 영웅 {heroId} 업데이트 완료 (Level: {level}, exp: {exp}, isUnlock = {isUnlock})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Firestore] 영웅 업데이트 실패: {e.Message}");
        }
    }

    // 지갑 업데이트
    public async Task UpdateWalletAsync(string uuid, int gold)
    {
        var docRef = _firestore.Collection(COLLECTION_NAME).Document(uuid)
            .Collection(COLLECTION_WALLET).Document(DOCUMENT_WALLET);

        await _firestore.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(docRef);
            var currentGold = snapshot.GetValue<int>("gold");

            transaction.Update(docRef, new Dictionary<string, object>
            {
                { "gold", currentGold + gold }
            });
        });
    }
    #endregion

    // 닉네임 중복 체크
    public async Task<bool> IsNicknameDuplicateAsync(string nickname)
    {
        var query = await _firestore.
            Collection(COLLECTION_PROFILE).
            WhereEqualTo("nickName", nickname)
            .GetSnapshotAsync();

        return query.Count > 0;
    }
}
