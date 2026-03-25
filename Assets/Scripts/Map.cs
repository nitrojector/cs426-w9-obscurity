/**
 * TODO:
 *
 * We allow players to drop a few markers (1, 2, 3, 4) to identify location for themselves.
 *
 * The map is a grid, defined by the texture coordinate as well
 *
 */
public class Map
{
	/// <summary>
	/// 0: air
	/// 1: collider
	/// 8: player start
	/// 9: goal for current level
	/// </summary>
	public static readonly byte[,] PrimaryLevelMapData =
	{
		//                  | <- 13
		{ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
		{ 1, 9, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1 },
		{ 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
		{ 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
		{ 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 1 },
		{ 1, 0, 1, 1, 0, 1, 0, 1, 1, 0, 1, 1, 1 },
		{ 1, 0, 1, 1, 0, 1, 8, 1, 1, 0, 0, 0, 1 }, // <- center
		{ 1, 0, 0, 0, 0, 1, 1, 1, 1, 0, 2, 0, 1 },
		{ 1, 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 1 },
		{ 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 0, 1, 1 },
		{ 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 0, 1, 1 },
		{ 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 1, 1 },
		{ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
	};
}