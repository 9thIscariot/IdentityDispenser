using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class RouletteManager : MonoBehaviour
{
    public static readonly string[] SinnerOrder =
    {
        "이상", "파우스트", "돈키호테", "료슈", "뫼르소", "홍루",
        "히스클리프", "이스마엘", "로쟈", "싱클레어", "오티스", "그레고르"
    };

    [Serializable]
    public sealed class PortraitBinding
    {
        public string identityId;
        public Texture2D texture;
    }

    [Header("추첨 범위")]
    [SerializeField] private bool yiSangOnly;
    [Tooltip("이미지가 연결된 인격만 추첨합니다.")]
    [SerializeField] private bool portraitsOnly = true;
    [Header("중복 허용")]
    [SerializeField] private bool allowSinnerDuplicates = true;
    [SerializeField] private bool allowIdentityDuplicates;
    [Header("인격 ID와 이미지 연결")]
    [SerializeField] private PortraitBinding[] portraits = Array.Empty<PortraitBinding>();

    [Header("슬롯 회전 시간 (초)")]
    [SerializeField, Min(0.05f)] private float cycleSeconds = 1f;
    [SerializeField, Min(0.1f)] private float brakeSeconds = 4f;

    private List<IdentityData> remaining;
    private int totalCount;
    private readonly Dictionary<string, Texture2D> portraitLookup = new Dictionary<string, Texture2D>();
    private readonly HashSet<string> drawnIdentityIds = new HashSet<string>();
    private readonly HashSet<string> drawnSinners = new HashSet<string>();
    private readonly HashSet<string> selectedSinners = new HashSet<string>(SinnerOrder);
    private RouletteAnimatior reel;
    private RoulletteUI view;
    public bool IsSpinning { get; private set; }
    public bool IsStopping { get; private set; }
    public int RemainingCount => remaining?.Count ?? 0;
    public IdentityData LastResult { get; private set; }
    public bool YiSangOnly => yiSangOnly;
    public bool PortraitsOnly => portraitsOnly;
    public bool AllowSinnerDuplicates => allowSinnerDuplicates;
    public bool AllowIdentityDuplicates => allowIdentityDuplicates;
    public bool IsSinnerSelected(string sinnerName) => selectedSinners.Contains(sinnerName);
    public event Action<IdentityData> ResultRevealed;

    private void Awake()
    {
        foreach (PortraitBinding binding in portraits)
        {
            if (binding != null && !string.IsNullOrEmpty(binding.identityId) && binding.texture != null)
                portraitLookup[binding.identityId] = binding.texture;
        }
        RestorePool();
        reel = GetComponent<RouletteAnimatior>();
        if (reel == null) reel = gameObject.AddComponent<RouletteAnimatior>();
        view = GetComponent<RoulletteUI>();
        if (view == null) view = gameObject.AddComponent<RoulletteUI>();
        view.Initialize(this);
        view.ShowIdle();
        Refresh("버튼을 눌러 인격을 뽑으세요.");
    }

    public void Spin()
    {
        if (!isActiveAndEnabled || IsSpinning || RemainingCount == 0) return;
        // 결과는 버튼을 누른 순간 균등 확률로 확정합니다. 연출에서는 다시 추첨하지 않습니다.
        var candidates = remaining.ToArray();
        int targetIndex = UnityEngine.Random.Range(0, candidates.Length);
        IdentityData result = candidates[targetIndex];
        IsSpinning = true;
        IsStopping = false;
        reel.enabled = true;
        Refresh("뽑기 중…");
        reel.Play(candidates, targetIndex, Mathf.Max(0.05f, cycleSeconds),
            Mathf.Max(0.1f, brakeSeconds),
            view.RenderReel,
            () => CompleteSpin(result),
            () => { IsSpinning = IsStopping = false; Refresh("뽑기가 중단되었습니다."); });
    }

    public void StopSpin()
    {
        if (!IsSpinning || IsStopping) return;
        IsStopping = true;
        reel.RequestStop();
        Refresh("뽑기 중…");
    }

    private void CompleteSpin(IdentityData result)
    {
        drawnIdentityIds.Add(result.Id);
        drawnSinners.Add(result.SinnerName);
        RebuildPool();
        LastResult = result;
        IsSpinning = false;
        IsStopping = false;
        view.ShowResult(result);
        Refresh(RemainingCount == 0 ? "모든 인격을 뽑았습니다. 초기화하면 다시 시작합니다." : "뽑기 완료 · 뽑힌 인격은 다음 뽑기에서 제외됩니다.");
        ResultRevealed?.Invoke(result);
    }

    public void ResetDraws()
    {
        if (!isActiveAndEnabled || IsSpinning) return;
        RestorePool();
        LastResult = null;
        view.ShowIdle();
        Refresh("전체 인격을 다시 추첨할 수 있습니다.");
    }

    public void SetSinnerDuplicates(bool allowed)
    {
        if (IsSpinning || allowSinnerDuplicates == allowed) return;
        allowSinnerDuplicates = allowed;
        RebuildPool();
        Refresh("수감자 중복 설정을 변경했습니다.");
    }

    public void SetIdentityDuplicates(bool allowed)
    {
        if (IsSpinning || allowIdentityDuplicates == allowed) return;
        allowIdentityDuplicates = allowed;
        RebuildPool();
        Refresh("인격 중복 설정을 변경했습니다.");
    }

    public void SetSinnerSelected(string sinnerName, bool selected)
    {
        if (IsSpinning || Array.IndexOf(SinnerOrder, sinnerName) < 0) return;
        if (selected ? !selectedSinners.Add(sinnerName) : !selectedSinners.Remove(sinnerName)) return;
        RebuildPool();
        Refresh("수감자 선택을 변경했습니다.");
    }

    private void Refresh(string message)
    {
        if (view != null)
            view.Refresh(RemainingCount, totalCount, IsSpinning, message);
    }

    private void RestorePool()
    {
        drawnIdentityIds.Clear();
        drawnSinners.Clear();
        RebuildPool();
    }

    private void RebuildPool()
    {
        remaining = IdentityDatabase.Default.CreateDrawPool(yiSangOnly ? "이상" : null);
        remaining.RemoveAll(identity => !selectedSinners.Contains(identity.SinnerName));
        if (portraitsOnly)
            remaining.RemoveAll(identity => !portraitLookup.ContainsKey(identity.Id));
        totalCount = remaining.Count;
        if (!allowSinnerDuplicates)
            remaining.RemoveAll(identity => drawnSinners.Contains(identity.SinnerName));
        if (!allowIdentityDuplicates)
            remaining.RemoveAll(identity => drawnIdentityIds.Contains(identity.Id));
    }

    public Texture2D GetPortrait(string identityId)
    {
        return portraitLookup.TryGetValue(identityId, out Texture2D texture) ? texture : null;
    }

    private void OnDisable()
    {
        if (reel != null) reel.Cancel();
    }

    public void QuitRoulette()
    {
        if (IsSpinning || LastResult != null)
        {
            if (IsSpinning) reel.Cancel();
            ResetDraws();
            return;
        }
        SceneManager.LoadScene("Title");
    }
}
