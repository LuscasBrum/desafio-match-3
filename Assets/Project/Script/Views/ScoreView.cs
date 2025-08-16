using UnityEngine;
using UnityEngine.UI;
using Gazeus.DesafioMatch3.Core;

public class ScoreView : MonoBehaviour
{
    [SerializeField] private Text _scoreText;

    private ScoreService _scoreService;

    public void Initialize(ScoreService scoreService)
    {
        _scoreService = scoreService;
        _scoreService.OnScoreChanged += UpdateScore;
        UpdateScore(_scoreService.CurrentScore);
    }

    private void OnDestroy()
    {
        if (_scoreService != null)
            _scoreService.OnScoreChanged -= UpdateScore;
    }

    private void UpdateScore(int newScore)
    {
        _scoreText.text = $"Score: {newScore}";
    }
}
