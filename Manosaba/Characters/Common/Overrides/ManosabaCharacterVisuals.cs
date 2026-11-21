using Godot;
using Manosaba.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;

namespace Manosaba.Characters.Common.Overrides
{
	public partial class ManosabaCharacterVisuals : NCreatureVisuals
	{
		private NCreature? _creatureNode;
		private Creature? _creature;
		private CanvasItem? _majokaVisual;
		private readonly Dictionary<CanvasItem, bool> _baseVisualVisibility = new();

		public override void _Ready()
		{
			base._Ready();

			_creatureNode = GetParent() as NCreature;
			_creature = _creatureNode?.Entity;
			if (_creature?.Player == null)
			{
				return;
			}

			_creature.CurrentHpChanged += OnHpChanged;
			_creature.Died += OnDied;
			_creature.Revived += OnRevived;

			InitializeMajokaVisuals();
			if (_majokaVisual != null)
			{
				_creature.PowerApplied += OnPowerAppliedOrRemoved;
				_creature.PowerIncreased += OnPowerIncreased;
				_creature.PowerDecreased += OnPowerDecreased;
				_creature.PowerRemoved += OnPowerAppliedOrRemoved;
			}

			ApplyCombatVisibility();
			ApplyMajokaVisualState();
		}

		public override void _ExitTree()
		{
			if (_creature != null)
			{
				_creature.CurrentHpChanged -= OnHpChanged;
				_creature.Died -= OnDied;
				_creature.Revived -= OnRevived;
				_creature.PowerApplied -= OnPowerAppliedOrRemoved;
				_creature.PowerIncreased -= OnPowerIncreased;
				_creature.PowerDecreased -= OnPowerDecreased;
				_creature.PowerRemoved -= OnPowerAppliedOrRemoved;
			}

			base._ExitTree();
		}

		private void OnHpChanged(int _, int __) => ApplyCombatVisibility();
		private void OnDied(Creature _) => ApplyCombatVisibility();
		private void OnRevived(Creature _) => ApplyCombatVisibility();
		private void OnPowerAppliedOrRemoved(PowerModel _) => ApplyMajokaVisualState();
		private void OnPowerIncreased(PowerModel _, int __, bool ___) => ApplyMajokaVisualState();
		private void OnPowerDecreased(PowerModel _, bool __) => ApplyMajokaVisualState();

		private void ApplyCombatVisibility()
		{
			if (_creatureNode == null || _creature == null)
			{
				return;
			}

			_creatureNode.Visible = IsCreatureVisible(_creature);
		}

		protected virtual bool IsCreatureVisible(Creature creature) =>
			RunManager.Instance.IsSingleplayerOrFakeMultiplayer || creature.IsAlive;

		protected virtual bool UsesMajokaPortrait(Creature creature) =>
			creature.GetPowerAmount<MajokaPower>() >= 100;

		private void InitializeMajokaVisuals()
		{
			_majokaVisual = GetNodeOrNull<CanvasItem>("Visuals/Majoka");
			if (_majokaVisual == null)
			{
				return;
			}

			CanvasItem? visuals = GetNodeOrNull<CanvasItem>("Visuals");
			if (visuals == null)
			{
				return;
			}

			foreach (Node child in visuals.GetChildren())
			{
				if (child is CanvasItem childVisual)
				{
					_baseVisualVisibility[childVisual] = childVisual.Visible;
				}
			}
		}

		private void ApplyMajokaVisualState()
		{
			if (_creature == null || _majokaVisual == null)
			{
				return;
			}

			bool isMajoka = UsesMajokaPortrait(_creature);
			foreach ((CanvasItem visual, bool baseVisible) in _baseVisualVisibility)
			{
				if (!GodotObject.IsInstanceValid(visual))
				{
					continue;
				}

				visual.Visible = visual == _majokaVisual ? isMajoka : !isMajoka && baseVisible;
			}
		}
	}
}
