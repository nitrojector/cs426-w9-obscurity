using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GoalTrigger : MonoBehaviour
{
    InvisibleMazeGame game;

    public void Init(InvisibleMazeGame owner) => game = owner;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (game == null) return;
        if (!other.CompareTag("Player")) return;
        game.OnGoalEntered();
    }
}
