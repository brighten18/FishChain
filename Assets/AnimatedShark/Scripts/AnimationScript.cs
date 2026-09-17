using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationScript : MonoBehaviour
{
    public Animator animator;
    private string currentState;

    const string PLAYER_IDLE = "Player_Idle";
    const string PLAYER_SWIM_CALM = "Player_Swim_Calm";
    const string PLAYER_SWIM_AGRESSIVE = "Player_Swim_Agressive";
    private void Start()
    {
        //stop animations on start
        animator.enabled = false;
    }
    void ChangeAnimationState(string newState)
    {
        //enable animator for animations
        animator.enabled = true;

        if (currentState == newState) return;

        animator.Play(newState);

        currentState = newState;
    }
    public void PlayerIdle()
    {
        ChangeAnimationState(PLAYER_IDLE);
    }
    public void PlayerSwimCalm()
    {
        ChangeAnimationState(PLAYER_SWIM_CALM);
    }
    public void PlayerSwimAngry()
    {
        ChangeAnimationState(PLAYER_SWIM_AGRESSIVE);
    }
}
