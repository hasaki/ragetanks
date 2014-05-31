namespace RageTanks
{
	public interface IScoreContainer
	{
		int Score { get; }
		void AddScore(int score);
	}
}