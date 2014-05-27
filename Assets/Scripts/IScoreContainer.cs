namespace Assets.Scripts
{
	public interface IScoreContainer
	{
		int Score { get; }
		void AddScore(int score);
	}
}