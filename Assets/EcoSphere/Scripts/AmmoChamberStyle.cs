using UnityEngine;
namespace EcoSphere
{
    // Shared presentation of the ammo chambers. The chamber count itself lives in WorldRules.ammoChambers.
    // Chambers sit in connected rows beside the cannon: the first rowLength fill rightward, the rest leftward.
    [CreateAssetMenu(menuName="EcoSphere/Ammo Chamber Style")]
    public sealed class AmmoChamberStyle : ScriptableObject
    {
        [Tooltip("Chamber artwork with a transparent window (max 256 px). Empty uses a generated placeholder ring.")]
        public Sprite frame;
        [Tooltip("Chamber frame width in world units; also the tap/click target.")]
        [Min(.1f)] public float frameWidth=1.5f;
        [Tooltip("Held sphere size relative to the loaded cannon sphere.")]
        [Range(.3f,1)] public float sphereScale=.85f;
        [Tooltip("First chamber center relative to the cannon pivot (mirrored for the left row), in world units.")]
        public Vector2 firstOffset=new Vector2(2.45f,.15f);
        [Tooltip("How far each chamber's left connector tucks behind the piece before it (cradle or previous chamber), in world units.")]
        [Min(0)] public float connectorOverlap=.06f;
        [Tooltip("Chambers per side before the row continues on the cannon's other side.")]
        [Min(1)] public int rowLength=2;
        [Min(0)] public float turnDuration=.16f;
        [Header("Cannon")]
        [Tooltip("Cannon artwork size (shell, arms, backing) relative to its original size. The loaded sphere keeps gameplay size.")]
        [Range(.5f,1.2f)] public float cannonScale=.9f;
        [Tooltip("Rotation pivot height above the cannon base, in world units (negative = below). The base is the artwork origin.")]
        public float pivotHeight;
        [Tooltip("Maximum aim angle either side of straight up, in degrees.")]
        [Range(10,85)] public float maxAimAngle=65;
        [Tooltip("Moves the whole cannon (artwork, pivot, loaded sphere) up or down against the stationary cradle, in world units.")]
        public float cannonVerticalOffset;
        public float LoadedLift=>GameplayGeometry.CannonBodyLift*cannonScale-pivotHeight;
        public Vector2 Pivot=>GameplayGeometry.CannonBase+Vector2.up*(pivotHeight+cannonVerticalOffset);
        // Shots leave from the loaded sphere, which swings with the cannon around its pivot.
        public Vector2 ShotOrigin(Vector2 direction)=>Pivot+direction.normalized*LoadedLift;

        [Header("Cannon cradle (stationary; chambers attach to its right connector)")]
        public Sprite cradle;
        [Min(.1f)] public float cradleWidth=4.4f;
        [Tooltip("Center of the cradle's U (where the cannon body sits), normalized from the sprite's bottom-left.")]
        public Vector2 cradlePivot=new Vector2(.42f,.92f);
        [Tooltip("Where the cradle pivot sits relative to the cannon pivot, in world units (the cannon body's center).")]
        public Vector2 cradleOffset=new Vector2(0,.55f);
        [Tooltip("Outer edge of the cradle's right connector, normalized from the sprite's bottom-left.")]
        public Vector2 cradleConnector=new Vector2(1,.671f);
        [Tooltip("Render order relative to the cannon (backing 2, arms 5, shell 7).")]
        public int cradleSortingOrder=1;
        [Tooltip("Local depth on the mount: positive sits behind the cannon, negative in front of it.")]
        public float cradleDepth=.4f;
        public Color placeholderColor=new Color(.45f,.7f,.95f,.85f);

        static AmmoChamberStyle current;
        public static AmmoChamberStyle Current
        {
            get
            {
                if(current)return current;
                current=Resources.Load<AmmoChamberStyle>("AmmoChamberStyle");
                if(!current){current=CreateInstance<AmmoChamberStyle>();current.hideFlags=HideFlags.DontSave;}
                return current;
            }
        }

        public float CradleHeight=>cradle ? cradleWidth*cradle.rect.height/cradle.rect.width : 0;
        // Sprite center relative to the cannon pivot, placing cradlePivot at cradleOffset.
        public Vector2 CradleCenter=>cradleOffset-new Vector2((cradlePivot.x-.5f)*cradleWidth,(cradlePivot.y-.5f)*CradleHeight);

        // Right-hand chambers chain from the cradle's connector when a cradle exists; otherwise from firstOffset.
        public Vector2 ChamberOffset(int chamber)
        {
            float step=frameWidth-connectorOverlap;
            if(chamber<rowLength)
            {
                Vector2 first=cradle
                    ? CradleCenter+new Vector2((cradleConnector.x-.5f)*cradleWidth,(cradleConnector.y-.5f)*CradleHeight)+new Vector2(frameWidth*.5f-connectorOverlap,0)
                    : firstOffset;
                return first+new Vector2(chamber*step,0);
            }
            return new Vector2(-(firstOffset.x+(chamber-rowLength)*step),firstOffset.y);
        }
    }
}
