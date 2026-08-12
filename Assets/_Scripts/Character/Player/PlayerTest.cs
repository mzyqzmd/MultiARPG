using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class PlayerTest : MonoBehaviour
{
    [SerializeField] private Player _player;

    private void Awake()
    {
        //NOTE:测试用订阅（仅本地玩家，避免远端副本误触发）
        Services.Get<InputService>().OnTestButtonClicked += TestButtonClicked;
    }

    private void OnDestroy()
    {
        //NOTE:测试用反订阅（与 OnNetworkSpawn 对称）
        Services.Get<InputService>().OnTestButtonClicked -= TestButtonClicked;
    }

    /// <summary>测试用：本地玩家点击测试按钮，仅本地玩家触发。</summary>
    private void TestButtonClicked()
    {
        if (!_player.IsOwner) return;
        Debug.Log("TestButtonClicked");
        //ApplyEffect("StatModifierEffect");
        //ApplyEffect("DamageEffect");
        //_stateControl.CombatLocked = !_stateControl.CombatLocked;
        Services.Get<SceneControlService>().ReturnToMainMenu();
        //ApplyEffect("HealEffect");
    }
}
