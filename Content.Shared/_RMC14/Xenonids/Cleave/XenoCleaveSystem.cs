using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Shields;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared.Coordinates;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Cleave;

public sealed class XenoCleaveSystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly VanguardShieldSystem _vanguard = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoCleaveComponent, XenoCleaveActionEvent>(OnCleaveAction);
    }

    private void OnCleaveAction(Entity<XenoCleaveComponent> xeno, ref XenoCleaveActionEvent args)
    {
        if (!_xeno.CanAbilityAttackTarget(xeno, args.Target))
            return;

        if (args.Handled)
            return;

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        var buffed = _vanguard.ShieldBuff(xeno);

        args.Handled = true;

        _rmcMelee.DoLunge(xeno, args.Target);

        if (args.Flings)
        {
            var flingRange = buffed ? xeno.Comp.FlingDistanceBuffed : xeno.Comp.FlingDistance;

            if (_sizeStun.TryGetSize(args.Target, out var size) && size >= RMCSizes.Big)
                flingRange *= 0.1f; //Big Xenos get flung less

            _rmcPulling.TryStopAllPullsFromAndOn(args.Target);

            //From fling
            var origin = _transform.GetMapCoordinates(xeno);

            _sizeStun.KnockBack(args.Target, origin, flingRange, flingRange, 10, true);

            if (_net.IsServer)
            {
                SpawnAttachedTo(xeno.Comp.FlingEffect, args.Target.ToCoordinates());
            }
        }
        else
        {
            var rootTime = buffed ? xeno.Comp.RootTimeBuffed : xeno.Comp.RootTime;

            _slow.TryRoot(args.Target, _xeno.TryApplyXenoDebuffMultiplier(args.Target, rootTime));

            if (_net.IsServer)
            {
                SpawnAttachedTo(xeno.Comp.RootEffect, args.Target.ToCoordinates());
            }
        }
    }
}
