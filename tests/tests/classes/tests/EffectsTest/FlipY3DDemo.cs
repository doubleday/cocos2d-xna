using cocos2d;

namespace tests
{
    public class FlipY3DDemo : CCFlipY3D
    {
        public static CCActionInterval actionWithDuration(float t)
        {
            CCFlipX3D flipx = Create(t);
            CCFiniteTimeAction flipx_back = flipx.Reverse();
            CCDelayTime delay = CCDelayTime.Create(2);

            return (CCSequence.Create(flipx, delay, flipx_back));
        }
    }
}