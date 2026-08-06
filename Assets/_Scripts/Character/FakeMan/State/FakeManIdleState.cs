using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeManIdleState : IState
{
    private readonly Animator _anim;
    public FakeManIdleState(Animator anim_)
    {
        _anim = anim_;
    }

    public void Enter()
    {
        _anim.SetBool("IdleState", true);
    }

    public void Exit()
    {
        _anim.SetBool("IdleState", false);
    }

    public void Update()
    {
    }
}
