using Unity.Entities;

namespace NSprites
{
    // Aspects were removed in Entities 6.x. This is the direct-component-access replacement:
    // a lightweight struct that holds the same RefRW/RefRO references the old aspect did,
    // built from ComponentLookup<T>s for a given entity. The public method surface
    // (SetAnimation/SetToFrame/ResetAnimation) and behavior are unchanged.
    public readonly struct AnimatorAspect
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly Entity _entity;
#endif
        private readonly RefRW<AnimationIndex> _animationIndex;
        private readonly RefRW<AnimationTimer> _animationTimer;
        private readonly RefRW<FrameIndex> _frameIndex;
        private readonly RefRO<AnimationSetLink> _animationSetLink;

        public AnimatorAspect(Entity entity,
            ref ComponentLookup<AnimationIndex> animationIndexLookup,
            ref ComponentLookup<AnimationTimer> animationTimerLookup,
            ref ComponentLookup<FrameIndex> frameIndexLookup,
            ref ComponentLookup<AnimationSetLink> animationSetLinkLookup)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _entity = entity;
#endif
            _animationIndex = animationIndexLookup.GetRefRW(entity);
            _animationTimer = animationTimerLookup.GetRefRW(entity);
            _frameIndex = frameIndexLookup.GetRefRW(entity);
            _animationSetLink = animationSetLinkLookup.GetRefRO(entity);
        }

        public void SetAnimation(int toAnimationIndex, in double worldTime)
        {
            // find animation by animation ID
            ref var animSet = ref _animationSetLink.ValueRO.value.Value;
            var setToAnimIndex = -1;
            for (int i = 0; i < animSet.Length; i++)
                if (animSet[i].ID == toAnimationIndex)
                {
                    setToAnimIndex = i;
                    break;
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (setToAnimIndex == -1)
                throw new NSpritesException($"{nameof(AnimatorAspect)}.{nameof(SetAnimation)}: incorrect {nameof(toAnimationIndex)} was passed. {_entity} has no animation with such ID ({toAnimationIndex}) was found");
#endif

            if (_animationIndex.ValueRO.value != setToAnimIndex)
            {
                ref var animData = ref animSet[setToAnimIndex];
                _animationIndex.ValueRW.value = setToAnimIndex;
                // here we want to set last frame and timer to 0 (equal to current time) to force animation system instantly switch
                // animation to 1st frame after we've modified it
                _frameIndex.ValueRW.value = animData.FrameDurations.Length - 1;
                _animationTimer.ValueRW.value = worldTime;
            }
        }

        public void SetToFrame(int frameIndex, in double worldTime)
        {
            ref var animData = ref _animationSetLink.ValueRO.value.Value[_animationIndex.ValueRO.value];
            _frameIndex.ValueRW.value = frameIndex;
            _animationTimer.ValueRW.value = worldTime + animData.FrameDurations[frameIndex];
        }

        public void ResetAnimation(in double worldTime) =>
            SetToFrame(0, worldTime);
    }
}
