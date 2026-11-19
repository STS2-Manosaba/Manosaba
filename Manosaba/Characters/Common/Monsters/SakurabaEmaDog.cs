using BaseLib.Abstracts;
using manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Manosaba.Characters.Common.Monsters
{
    public sealed class SakurabaEmaDog : CustomMonsterModel
    {
        public override int MinInitialHp => 1;
        public override int MaxInitialHp => 1;
        public override string? HurtSfx => "event:/Manosaba/audio/monsters/sakuraba_ema_dog_hurt.ogg";

        /// <summary>寵物出手（<c>CreatureCmd.TriggerAnim(ema, "Attack", …)</c>）時播放；可之後換成專用吠叫/咬擊檔。</summary>
        public override string? CustomAttackSfx => "event:/Manosaba/audio/monsters/sakuraba_ema_dog_hurt.ogg";

        public override string? CustomVisualPath => "sakuraba_ema_dog.tscn".MonsterScenePath();

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            MoveState moveState = new MoveState("NOTHING_MOVE", (IReadOnlyList<Creature> _) => Task.CompletedTask);
            moveState.FollowUpState = moveState;
            return new MonsterMoveStateMachine([moveState], moveState);
        }
    }
}
