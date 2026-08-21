using UnityEngine;

/// <summary>
/// 假人表现配置：仅继承基类（受击 VFX/受伤音/血溅音/idle 音），无死亡音与攻击音（假人为木桩，不死亡、不攻击）。
/// </summary>
[CreateAssetMenu(fileName = "FakeManPresentation", menuName = "MultiARPG/Presentation/FakeManPresentation")] 
public class SO_FakeManPresentation : SO_CharacterPresentation
{
}
