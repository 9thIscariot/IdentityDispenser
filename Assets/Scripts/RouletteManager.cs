using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RouletteManager : MonoBehaviour
{
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
    [Header("인격 ID와 이미지 연결")]
    [SerializeField] private PortraitBinding[] portraits = Array.Empty<PortraitBinding>();

    [Header("슬롯 회전 시간 (초)")]
    [SerializeField, Min(0.05f)] private float cycleSeconds = 1f;
    [SerializeField, Min(0.1f)] private float minimumCruiseSeconds = 1f;
    [SerializeField, Min(0.1f)] private float brakeSeconds = 4f;

    private List<IdentityData> remaining;
    private int totalCount;
    private readonly Dictionary<string, Texture2D> portraitLookup = new Dictionary<string, Texture2D>();
    private RouletteAnimatior reel;
    private RoulletteUI view;
    public bool IsSpinning { get; private set; }
    public int RemainingCount => remaining?.Count ?? 0;
    public IdentityData LastResult { get; private set; }
    public bool YiSangOnly => yiSangOnly;
    public bool PortraitsOnly => portraitsOnly;
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
        reel.enabled = true;
        Refresh("추첨 중…");
        reel.Play(candidates, targetIndex, Mathf.Max(0.05f, cycleSeconds),
            Mathf.Max(0.1f, minimumCruiseSeconds), Mathf.Max(0.1f, brakeSeconds),
            view.RenderReel,
            () => CompleteSpin(result),
            () => { IsSpinning = false; Refresh("추첨이 중단되었습니다. 다시 뽑을 수 있습니다."); });
    }

    private void CompleteSpin(IdentityData result)
    {
        remaining.Remove(result);
        LastResult = result;
        IsSpinning = false;
        view.ShowResult(result);
        Refresh(RemainingCount == 0 ? "모든 인격을 뽑았습니다. 초기화하면 다시 시작합니다." : "추첨 완료 · 뽑힌 인격은 다음 추첨에서 제외됩니다.");
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

    private void Refresh(string message)
    {
        if (view != null)
            view.Refresh(RemainingCount, totalCount, IsSpinning, message);
    }

    private void RestorePool()
    {
        remaining = IdentityDatabase.Default.CreateDrawPool(yiSangOnly ? "이상" : null);
        if (portraitsOnly)
            remaining.RemoveAll(identity => !portraitLookup.ContainsKey(identity.Id));
        totalCount = remaining.Count;
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
        if (reel != null) reel.Cancel();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
