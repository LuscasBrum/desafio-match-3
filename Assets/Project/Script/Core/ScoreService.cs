using System;

namespace Gazeus.DesafioMatch3.Core
{
    public class ScoreService
    {
        public event Action<int> OnScoreChanged;

        private int _score;

        public int CurrentScore => _score;

        public void Reset()
        {
            _score = 0;
            OnScoreChanged?.Invoke(_score);
        }

        public void AddPoints(int basePoints, int matchedCount, int cascadeLevel)
        {
            int score = basePoints * matchedCount;

            if (matchedCount > 3)
                score += (matchedCount - 3) * basePoints * 2;

            float mult = 1f + (0.25f * cascadeLevel);
            score = (int)(score * mult);

            _score += score;
            OnScoreChanged?.Invoke(_score);
        }
    }
}
