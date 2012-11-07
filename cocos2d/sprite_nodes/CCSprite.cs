using System;
using System.Diagnostics;

namespace cocos2d
{
    public class CCSprite : CCNode, ICCTextureProtocol, ICCRGBAProtocol
    {
        protected bool m_bDirty; // Sprite needs to be updated
        protected bool m_bFlipX;
        protected bool m_bFlipY;
        protected bool m_bHasChildren; // optimization to check if it contain children
        protected bool m_bOpacityModifyRGB;
        protected bool m_bRectRotated;
        protected bool m_bRecursiveDirty; // Subchildren needs to be updated
        protected bool m_bShouldBeHidden; // should not be drawn because one of the ancestors is not visible
        protected byte m_nOpacity;

        // Offset Position (used by Zwoptex)
        protected CCPoint m_obOffsetPosition;

        protected CCRect m_obRect;
        protected CCPoint m_obUnflippedOffsetPositionFromCenter;
        protected CCSpriteBatchNode m_pobBatchNode; // Used batch node (weak reference)
        protected CCTexture2D m_pobTexture; // Texture used to render the sprite
        protected CCTextureAtlas m_pobTextureAtlas; // Sprite Sheet texture atlas (weak reference)
        protected ccBlendFunc m_sBlendFunc; // Needed for the texture protocol
        protected ccColor3B m_sColor;

        // vertex coords, texture coords and color info

        // opacity and RGB protocol
        protected ccColor3B m_sColorUnmodified;
        internal ccV3F_C4B_T2F_Quad m_sQuad;
        protected CCAffineTransform m_transformToBatch; //
        protected int m_uAtlasIndex; // Absolute (real) Index on the SpriteSheet

        public virtual bool Dirty
        {
            get { return m_bDirty; }
            set { m_bDirty = value; }
        }

        public ccV3F_C4B_T2F_Quad Quad
        {
            // read only
            get { return m_sQuad; }
        }


        public bool IsTextureRectRotated
        {
            get { return m_bRectRotated; }
        }

        public int AtlasIndex
        {
            get { return m_uAtlasIndex; }
            set { m_uAtlasIndex = value; }
        }

        public CCRect TextureRect
        {
            get { return m_obRect; }
            set { SetTextureRect(value, false, value.size); }
        }

        public CCPoint OffsetPosition
        {
            // read only
            get { return m_obOffsetPosition; }
        }

        public override CCPoint Position
        {
            get { return base.Position; }
            set
            {
                base.Position = value;
                SET_DIRTY_RECURSIVELY();
            }
        }

        public override float Rotation
        {
            get { return base.Rotation; }
            set
            {
                base.Rotation = value;
                SET_DIRTY_RECURSIVELY();
            }
        }

        public override float SkewX
        {
            get { return base.SkewX; }
            set
            {
                base.SkewX = value;
                SET_DIRTY_RECURSIVELY();
            }
        }

        public override float SkewY
        {
            get { return base.SkewY; }
            set
            {
                base.SkewY = value;
                SET_DIRTY_RECURSIVELY();
            }
        }

        public override float ScaleX
        {
            get { return base.ScaleX; }
            set
            {
                base.ScaleX = value;
                SET_DIRTY_RECURSIVELY();
            }
        }

        public override float ScaleY
        {
            get { return base.ScaleY; }
            set
            {
                base.ScaleY = value;
                SET_DIRTY_RECURSIVELY();
            }
        }

        public override float Scale
        {
            get { return base.Scale; }
            set
            {
                base.Scale = value;
                SET_DIRTY_RECURSIVELY();
            }
        }

        public override float VertexZ
        {
            get { return base.VertexZ; }
            set
            {
                base.VertexZ = value;
                SET_DIRTY_RECURSIVELY();
            }
        }

        public override CCPoint AnchorPoint
        {
            get { return base.AnchorPoint; }
            set
            {
                base.AnchorPoint = value;
                SET_DIRTY_RECURSIVELY();
            }
        }

        public override bool IgnoreAnchorPointForPosition
        {
            get { return base.IgnoreAnchorPointForPosition; }
            set
            {
                Debug.Assert(m_pobBatchNode == null, "ignoreAnchorPointForPosition is invalid in CCSprite");
                base.IgnoreAnchorPointForPosition = value;
                SET_DIRTY_RECURSIVELY();
            }
        }

        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                base.Visible = value;
                SET_DIRTY_RECURSIVELY();
            }
        }

        public bool FlipX
        {
            get { return m_bFlipX; }
            set
            {
                if (m_bFlipX != value)
                {
                    m_bFlipX = value;
                    SetTextureRect(m_obRect, m_bRectRotated, m_tContentSize);
                }
            }
        }

        public bool FlipY
        {
            get { return m_bFlipY; }
            set
            {
                if (m_bFlipY != value)
                {
                    m_bFlipY = value;
                    SetTextureRect(m_obRect, m_bRectRotated, m_tContentSize);
                }
            }
        }

        public CCSpriteFrame DisplayFrame
        {
            get
            {
                return CCSpriteFrame.Create(
                    m_pobTexture,
                    ccMacros.CC_RECT_POINTS_TO_PIXELS(m_obRect),
                    m_bRectRotated,
                    ccMacros.CC_POINT_POINTS_TO_PIXELS(m_obUnflippedOffsetPositionFromCenter),
                    ccMacros.CC_SIZE_POINTS_TO_PIXELS(m_tContentSize)
                    );
            }
            set
            {
                m_obUnflippedOffsetPositionFromCenter = value.Offset;

                CCTexture2D pNewTexture = value.Texture;
                // update texture before updating texture rect
                if (pNewTexture != m_pobTexture)
                {
                    Texture = pNewTexture;
                }

                // update rect
                m_bRectRotated = value.IsRotated;
                SetTextureRect(value.Rect, m_bRectRotated, value.OriginalSize);
            }
        }

        public CCSpriteBatchNode BatchNode
        {
            get { return m_pobBatchNode; }
            set
            {
                m_pobBatchNode = value;

                if (value == null)
                {
                    m_uAtlasIndex = ccMacros.CCSpriteIndexNotInitialized;
                    m_pobTextureAtlas = null;
                    m_bRecursiveDirty = false;
                    Dirty = false;

                    float x1 = m_obOffsetPosition.x;
                    float y1 = m_obOffsetPosition.y;
                    float x2 = x1 + m_obRect.size.width;
                    float y2 = y1 + m_obRect.size.height;

                    m_sQuad.bl.vertices = new ccVertex3F(x1, y1, 0);
                    m_sQuad.br.vertices = new ccVertex3F(x2, y1, 0);
                    m_sQuad.tl.vertices = new ccVertex3F(x1, y2, 0);
                    m_sQuad.tr.vertices = new ccVertex3F(x2, y2, 0);
                }
                else
                {
                    // using batch
                    m_transformToBatch = CCAffineTransform.CCAffineTransformMakeIdentity();
                    m_pobTextureAtlas = m_pobBatchNode.TextureAtlas; // weak ref
                }
            }
        }

        #region ICCRGBAProtocol Members

        public byte Opacity
        {
            get { return m_nOpacity; }
            set
            {
                m_nOpacity = value;

                // special opacity for premultiplied textures
                if (m_bOpacityModifyRGB)
                {
                    Color = m_sColorUnmodified;
                }

                UpdateColor();
            }
        }

        public ccColor3B Color
        {
            get
            {
                if (m_bOpacityModifyRGB)
                {
                    return m_sColorUnmodified;
                }
                return m_sColor;
            }
            set
            {
                m_sColor = new ccColor3B(value.r, value.g, value.b);
                m_sColorUnmodified = new ccColor3B(value.r, value.g, value.b);

                if (m_bOpacityModifyRGB)
                {
                    m_sColor.r = (byte) (value.r * m_nOpacity / 255f);
                    m_sColor.g = (byte) (value.g * m_nOpacity / 255f);
                    m_sColor.b = (byte) (value.b * m_nOpacity / 255f);
                }

                UpdateColor();
            }
        }

        public virtual bool IsOpacityModifyRGB
        {
            get { return m_bOpacityModifyRGB; }
            set
            {
                ccColor3B oldColor = m_sColor;
                m_bOpacityModifyRGB = value;
                m_sColor = oldColor;
            }
        }

        #endregion

        #region ICCTextureProtocol Members

        public ccBlendFunc BlendFunc
        {
            get { return m_sBlendFunc; }
            set { m_sBlendFunc = value; }
        }

        public virtual CCTexture2D Texture
        {
            get { return m_pobTexture; }
            set
            {
                // If batchnode, then texture id should be the same
                Debug.Assert(m_pobBatchNode == null || value.Name == m_pobBatchNode.Texture.Name,
                             "CCSprite: Batched sprites should use the same texture as the batchnode");

                if (m_pobBatchNode == null && m_pobTexture != value)
                {
                    m_pobTexture = value;
                    UpdateBlendFunc();
                }
            }
        }

        #endregion

        public static CCSprite Create(CCTexture2D texture)
        {
            var sprite = new CCSprite();
            if (sprite.InitWithTexture(texture))
            {
                return sprite;
            }
            return null;
        }

        public static CCSprite Create(CCTexture2D texture, CCRect rect)
        {
            var sprite = new CCSprite();
            if (sprite.InitWithTexture(texture, rect))
            {
                return sprite;
            }
            return null;
        }

        public static CCSprite Create(string fileName)
        {
            var sprite = new CCSprite();
            if (sprite.InitWithFile(fileName))
            {
                return sprite;
            }

            return null;
        }

        public static CCSprite Create(string fileName, CCRect rect)
        {
            var sprite = new CCSprite();
            if (sprite.InitWithFile(fileName, rect))
            {
                return sprite;
            }
            return null;
        }

        public static CCSprite Create(CCSpriteFrame pSpriteFrame)
        {
            var pobSprite = new CCSprite();
            if (pobSprite.InitWithSpriteFrame(pSpriteFrame))
            {
                return pobSprite;
            }
            return null;
        }

        public new static CCSprite Create()
        {
            var pobSprite = new CCSprite();
            if (pobSprite.Init())
            {
                return pobSprite;
            }
            return null;
        }

        public virtual bool Init()
        {
            return InitWithTexture(null, new CCRect());
        }

        public bool InitWithTexture(CCTexture2D pTexture, CCRect rect, bool rotated)
        {
            m_pobBatchNode = null;

            // shader program
            //setShaderProgram(CCShaderCache::sharedShaderCache()->programForKey(kCCShader_PositionTextureColor));

            m_bRecursiveDirty = false;
            Dirty = false;

            m_bOpacityModifyRGB = true;
            m_nOpacity = 255;
            m_sColor = m_sColorUnmodified = ccTypes.ccWHITE;

            m_sBlendFunc.src = ccMacros.CC_BLEND_SRC;
            m_sBlendFunc.dst = ccMacros.CC_BLEND_DST;

            m_bFlipX = m_bFlipY = false;

            // default transform anchor: center
            AnchorPoint = CCPointExtension.ccp(0.5f, 0.5f);

            // zwoptex default values
            m_obOffsetPosition = CCPoint.Zero;

            m_bHasChildren = false;

            // clean the Quad
            m_sQuad = new ccV3F_C4B_T2F_Quad();

            // Atlas: Color
            var tmpColor = new ccColor4B(255, 255, 255, 255);
            m_sQuad.bl.colors = tmpColor;
            m_sQuad.br.colors = tmpColor;
            m_sQuad.tl.colors = tmpColor;
            m_sQuad.tr.colors = tmpColor;

            // update texture (calls updateBlendFunc)
            Texture = pTexture;
            SetTextureRect(rect, rotated, rect.size);

            // by default use "Self Render".
            // if the sprite is added to a batchnode, then it will automatically switch to "batchnode Render"
            BatchNode = null;

            return true;
        }

        public bool InitWithTexture(CCTexture2D texture, CCRect rect)
        {
            return InitWithTexture(texture, rect, false);
        }

        public bool InitWithTexture(CCTexture2D texture)
        {
            Debug.Assert(texture != null, "Invalid texture for sprite");

            var rect = new CCRect();
            rect.size = texture.ContentSize;

            return InitWithTexture(texture, rect);
        }

        public bool InitWithFile(string fileName)
        {
            Debug.Assert(!String.IsNullOrEmpty(fileName), "Invalid filename for sprite");

            CCSpriteFrame pFrame = CCSpriteFrameCache.SharedSpriteFrameCache.SpriteFrameByName(fileName);
            if (pFrame != null)
            {
                return InitWithSpriteFrame(pFrame);
            }

            CCTexture2D pTexture = CCTextureCache.SharedTextureCache.AddImage(fileName);

            if (null != pTexture)
            {
                var rect = new CCRect();
                rect.size = pTexture.ContentSize;
                return InitWithTexture(pTexture, rect);
            }

            return false;
        }

        public bool InitWithFile(string fileName, CCRect rect)
        {
            Debug.Assert(!String.IsNullOrEmpty(fileName), "Invalid filename for sprite");

            CCTexture2D pTexture = CCTextureCache.SharedTextureCache.AddImage(fileName);
            if (pTexture != null)
            {
                return InitWithTexture(pTexture, rect);
            }

            return false;
        }

        public bool InitWithSpriteFrame(CCSpriteFrame pSpriteFrame)
        {
            Debug.Assert(pSpriteFrame != null);

            bool bRet = InitWithTexture(pSpriteFrame.Texture, pSpriteFrame.Rect);
            DisplayFrame = pSpriteFrame;

            return bRet;
        }

        public void SetTextureRect(CCRect rect)
        {
            SetTextureRect(rect, false, rect.size);
        }

        public void SetTextureRect(CCRect value, bool rotated, CCSize untrimmedSize)
        {
            m_bRectRotated = rotated;

            ContentSize = untrimmedSize;
            SetVertexRect(value);
            SetTextureCoords(value);

            CCPoint relativeOffset = m_obUnflippedOffsetPositionFromCenter;

            // issue #732
            if (m_bFlipX)
            {
                relativeOffset.x = -relativeOffset.x;
            }
            if (m_bFlipY)
            {
                relativeOffset.y = -relativeOffset.y;
            }

            m_obOffsetPosition.x = relativeOffset.x + (m_tContentSize.width - m_obRect.size.width) / 2;
            m_obOffsetPosition.y = relativeOffset.y + (m_tContentSize.height - m_obRect.size.height) / 2;

            // rendering using batch node
            if (m_pobBatchNode != null)
            {
                // update dirty_, don't update recursiveDirty_
                Dirty = true;
            }
            else
            {
                // self rendering

                // Atlas: Vertex
                float x1 = 0 + m_obOffsetPosition.x;
                float y1 = 0 + m_obOffsetPosition.y;
                float x2 = x1 + m_obRect.size.width;
                float y2 = y1 + m_obRect.size.height;

                // Don't update Z.
                m_sQuad.bl.vertices = ccTypes.vertex3(x1, y1, 0);
                m_sQuad.br.vertices = ccTypes.vertex3(x2, y1, 0);
                m_sQuad.tl.vertices = ccTypes.vertex3(x1, y2, 0);
                m_sQuad.tr.vertices = ccTypes.vertex3(x2, y2, 0);
            }
        }

        // override this method to generate "double scale" sprites
        protected virtual void SetVertexRect(CCRect rect)
        {
            m_obRect = rect;
        }

        private void SetTextureCoords(CCRect rect)
        {
            rect = ccMacros.CC_RECT_POINTS_TO_PIXELS(rect);

            CCTexture2D tex = m_pobBatchNode != null ? m_pobTextureAtlas.Texture : m_pobTexture;
            if (tex == null)
            {
                return;
            }

            float atlasWidth = tex.PixelsWide;
            float atlasHeight = tex.PixelsHigh;

            float left, right, top, bottom;

            if (m_bRectRotated)
            {
#if CC_FIX_ARTIFACTS_BY_STRECHING_TEXEL
                left    = (2*rect.origin.x+1)/(2*atlasWidth);
                right    = left+(rect.size.height*2-2)/(2*atlasWidth);
                top        = (2*rect.origin.y+1)/(2*atlasHeight);
                bottom    = top+(rect.size.width*2-2)/(2*atlasHeight);
#else
                left = rect.origin.x / atlasWidth;
                right = (rect.origin.x + rect.size.height) / atlasWidth;
                top = rect.origin.y / atlasHeight;
                bottom = (rect.origin.y + rect.size.width) / atlasHeight;
#endif
                // CC_FIX_ARTIFACTS_BY_STRECHING_TEXEL

                if (m_bFlipX)
                {
                    ccMacros.CC_SWAP(ref top, ref bottom);
                }

                if (m_bFlipY)
                {
                    ccMacros.CC_SWAP(ref left, ref right);
                }

                m_sQuad.bl.texCoords.u = left;
                m_sQuad.bl.texCoords.v = top;
                m_sQuad.br.texCoords.u = left;
                m_sQuad.br.texCoords.v = bottom;
                m_sQuad.tl.texCoords.u = right;
                m_sQuad.tl.texCoords.v = top;
                m_sQuad.tr.texCoords.u = right;
                m_sQuad.tr.texCoords.v = bottom;
            }
            else
            {
#if CC_FIX_ARTIFACTS_BY_STRECHING_TEXEL
                left    = (2*rect.origin.x+1)/(2*atlasWidth);
                right    = left + (rect.size.width*2-2)/(2*atlasWidth);
                top        = (2*rect.origin.y+1)/(2*atlasHeight);
                bottom    = top + (rect.size.height*2-2)/(2*atlasHeight);
#else
                left = rect.origin.x / atlasWidth;
                right = (rect.origin.x + rect.size.width) / atlasWidth;
                top = rect.origin.y / atlasHeight;
                bottom = (rect.origin.y + rect.size.height) / atlasHeight;
#endif
                // ! CC_FIX_ARTIFACTS_BY_STRECHING_TEXEL

                if (m_bFlipX)
                {
                    ccMacros.CC_SWAP(ref left, ref right);
                }

                if (m_bFlipY)
                {
                    ccMacros.CC_SWAP(ref top, ref bottom);
                }

                m_sQuad.bl.texCoords.u = left;
                m_sQuad.bl.texCoords.v = bottom;
                m_sQuad.br.texCoords.u = right;
                m_sQuad.br.texCoords.v = bottom;
                m_sQuad.tl.texCoords.u = left;
                m_sQuad.tl.texCoords.v = top;
                m_sQuad.tr.texCoords.u = right;
                m_sQuad.tr.texCoords.v = top;
            }
        }

        public void UpdateTransform()
        {
            Debug.Assert(m_pobBatchNode != null,
                         "updateTransform is only valid when CCSprite is being rendered using an CCSpriteBatchNode");

            // recaculate matrix only if it is dirty
            if (Dirty)
            {
                // If it is not visible, or one of its ancestors is not visible, then do nothing:
                if (!m_bIsVisible ||
                    (m_pParent != null && m_pParent != m_pobBatchNode && ((CCSprite) m_pParent).m_bShouldBeHidden))
                {
                    m_sQuad.br.vertices =
                        m_sQuad.tl.vertices = m_sQuad.tr.vertices = m_sQuad.bl.vertices = new ccVertex3F(0, 0, 0);
                    m_bShouldBeHidden = true;
                }
                else
                {
                    m_bShouldBeHidden = false;

                    if (m_pParent == null || m_pParent == m_pobBatchNode)
                    {
                        m_transformToBatch = NodeToParentTransform();
                    }
                    else
                    {
                        Debug.Assert((m_pParent as CCSprite) != null,
                                     "Logic error in CCSprite. Parent must be a CCSprite");
                        m_transformToBatch = CCAffineTransform.CCAffineTransformConcat(NodeToParentTransform(),
                                                                                       ((CCSprite) m_pParent).
                                                                                           m_transformToBatch);
                    }

                    //
                    // calculate the Quad based on the Affine Matrix
                    //

                    CCSize size = m_obRect.size;

                    float x1 = m_obOffsetPosition.x;
                    float y1 = m_obOffsetPosition.y;

                    float x2 = x1 + size.width;
                    float y2 = y1 + size.height;
                    float x = m_transformToBatch.tx;
                    float y = m_transformToBatch.ty;

                    float cr = m_transformToBatch.a;
                    float sr = m_transformToBatch.b;
                    float cr2 = m_transformToBatch.d;
                    float sr2 = -m_transformToBatch.c;
                    float ax = x1 * cr - y1 * sr2 + x;
                    float ay = x1 * sr + y1 * cr2 + y;

                    float bx = x2 * cr - y1 * sr2 + x;
                    float by = x2 * sr + y1 * cr2 + y;

                    float cx = x2 * cr - y2 * sr2 + x;
                    float cy = x2 * sr + y2 * cr2 + y;

                    float dx = x1 * cr - y2 * sr2 + x;
                    float dy = x1 * sr + y2 * cr2 + y;

                    m_sQuad.bl.vertices = new ccVertex3F(ax, ay, m_fVertexZ);
                    m_sQuad.br.vertices = new ccVertex3F(bx, by, m_fVertexZ);
                    m_sQuad.tl.vertices = new ccVertex3F(dx, dy, m_fVertexZ);
                    m_sQuad.tr.vertices = new ccVertex3F(cx, cy, m_fVertexZ);
                }

                m_pobTextureAtlas.UpdateQuad(ref m_sQuad, m_uAtlasIndex);
                m_bRecursiveDirty = false;
                m_bDirty = false;
            }

            // recursively iterate over children
            if (m_bHasChildren)
            {
                CCNode[] elements = m_pChildren.Elements;
                if (m_pobBatchNode != null)
                {
                    for (int i = 0, count = m_pChildren.count; i < count; i++)
                    {
                        ((CCSprite) elements[i]).UpdateTransform();
                    }
                }
                else
                {
                    for (int i = 0, count = m_pChildren.count; i < count; i++)
                    {
                        var sprite = elements[i] as CCSprite;
                        if (sprite != null)
                        {
                            sprite.UpdateTransform();
                        }
                    }
                }
            }
        }

        public override void Draw()
        {
            Debug.Assert(m_pobBatchNode == null);

            DrawManager.BlendFunc(m_sBlendFunc);
            DrawManager.BindTexture(Texture);
            DrawManager.DrawQuad(ref m_sQuad);

            /*
            var sb = DrawManager.spriteBatch;
            sb.Begin(SpriteSortMode.Deferred, null, null, null, null, null, DrawManager.basicEffect.World);
            sb.Draw(Texture.getTexture2D(), new Vector2(0, 0), new Color(this.Color.r, this.Color.g, this.Color.b, Opacity));
            sb.End();
            */
        }

        public override void AddChild(CCNode child, int zOrder, int tag)
        {
            Debug.Assert(child != null, "Argument must be non-NULL");

            if (m_pobBatchNode != null)
            {
                var sprite = child as CCSprite;

                Debug.Assert(sprite != null, "CCSprite only supports CCSprites as children when using CCSpriteBatchNode");
                Debug.Assert(sprite.Texture.Name == m_pobTextureAtlas.Texture.Name);

                m_pobBatchNode.AppendChild(sprite);

                if (!m_bReorderChildDirty)
                {
                    SetReorderChildDirtyRecursively();
                }
            }

            base.AddChild(child, zOrder, tag);
            m_bHasChildren = true;
        }

        public override void ReorderChild(CCNode child, int zOrder)
        {
            Debug.Assert(child != null);
            Debug.Assert(m_pChildren.Contains(child));

            if (zOrder == child.ZOrder)
            {
                return;
            }

            if (m_pobBatchNode != null && !m_bReorderChildDirty)
            {
                SetReorderChildDirtyRecursively();
                m_pobBatchNode.ReorderBatch(true);
            }

            base.ReorderChild(child, zOrder);
        }

        public override void RemoveChild(CCNode child, bool cleanup)
        {
            if (m_pobBatchNode != null)
            {
                m_pobBatchNode.RemoveSpriteFromAtlas((CCSprite) (child));
            }

            base.RemoveChild(child, cleanup);
        }

        public override void RemoveAllChildrenWithCleanup(bool cleanup)
        {
            if (m_pobBatchNode != null)
            {
                CCSpriteBatchNode batch = m_pobBatchNode;
                CCNode[] elements = m_pChildren.Elements;
                for (int i = 0, count = m_pChildren.count; i < count; i++)
                {
                    batch.RemoveSpriteFromAtlas((CCSprite) elements[i]);
                }
            }

            base.RemoveAllChildrenWithCleanup(cleanup);

            m_bHasChildren = false;
        }

        public override void SortAllChildren()
        {
            if (m_bReorderChildDirty)
            {
                int i, j, length = m_pChildren.count;
                CCNode[] x = m_pChildren.Elements;
                CCNode tempItem;

                // insertion sort
                for (i = 1; i < length; i++)
                {
                    tempItem = x[i];
                    j = i - 1;

                    //continue moving element downwards while zOrder is smaller or when zOrder is the same but orderOfArrival is smaller
                    while (j >= 0 &&
                           (tempItem.m_nZOrder < x[j].m_nZOrder ||
                            (tempItem.m_nZOrder == x[j].m_nZOrder && tempItem.m_nOrderOfArrival < x[j].m_nOrderOfArrival)))
                    {
                        x[j + 1] = x[j];
                        j = j - 1;
                    }
                    x[j + 1] = tempItem;
                }

                if (m_pobBatchNode != null)
                {
                    foreach (CCNode node in m_pChildren)
                    {
                        (node).SortAllChildren();
                    }
                }

                m_bReorderChildDirty = false;
            }
        }

        public virtual void SetReorderChildDirtyRecursively()
        {
            //only set parents flag the first time
            if (!m_bReorderChildDirty)
            {
                m_bReorderChildDirty = true;
                CCNode node = m_pParent;
                while (node != null && node != m_pobBatchNode)
                {
                    ((CCSprite) node).SetReorderChildDirtyRecursively();
                    node = node.Parent;
                }
            }
        }

        public virtual void SetDirtyRecursively(bool bValue)
        {
            m_bDirty = m_bRecursiveDirty = bValue;

            // recursively set dirty
            if (m_bHasChildren)
            {
                CCNode[] elements = m_pChildren.Elements;
                for (int i = 0, count = m_pChildren.count; i < count; i++)
                {
                    var sprite = elements[i] as CCSprite;
                    if (sprite != null)
                    {
                        sprite.SetDirtyRecursively(true);
                    }
                }
            }
        }

        private void SET_DIRTY_RECURSIVELY()
        {
            if (m_pobBatchNode != null && !m_bRecursiveDirty)
            {
                m_bDirty = m_bRecursiveDirty = true;
                if (m_bHasChildren)
                {
                    SetDirtyRecursively(true);
                }
            }
        }

        private void UpdateColor()
        {
            m_sQuad.bl.colors = new ccColor4B(m_sColor.r, m_sColor.g, m_sColor.b, m_nOpacity);
            m_sQuad.br.colors = new ccColor4B(m_sColor.r, m_sColor.g, m_sColor.b, m_nOpacity);
            m_sQuad.tl.colors = new ccColor4B(m_sColor.r, m_sColor.g, m_sColor.b, m_nOpacity);
            m_sQuad.tr.colors = new ccColor4B(m_sColor.r, m_sColor.g, m_sColor.b, m_nOpacity);

            // renders using Sprite Manager
            if (m_pobBatchNode != null)
            {
                if (m_uAtlasIndex != ccMacros.CCSpriteIndexNotInitialized)
                {
                    m_pobTextureAtlas.UpdateQuad(ref m_sQuad, m_uAtlasIndex);
                }
                else
                {
                    // no need to set it recursively
                    // update dirty_, don't update recursiveDirty_
                    m_bDirty = true;
                }
            }

            // self render
            // do nothing
        }

        public void SetDisplayFrameWithAnimationName(string animationName, int frameIndex)
        {
            Debug.Assert(!String.IsNullOrEmpty(animationName),
                         "CCSprite#setDisplayFrameWithAnimationName. animationName must not be NULL");

            CCAnimation a = CCAnimationCache.SharedAnimationCache.AnimationByName(animationName);

            Debug.Assert(a != null, "CCSprite#setDisplayFrameWithAnimationName: Frame not found");

            var frame = (CCAnimationFrame) a.Frames[frameIndex];

            Debug.Assert(frame != null, "CCSprite#setDisplayFrame. Invalid frame");

            DisplayFrame = frame.SpriteFrame;
        }

        public bool IsFrameDisplayed(CCSpriteFrame pFrame)
        {
            CCRect r = pFrame.Rect;

            return (
                       CCRect.CCRectEqualToRect(r, m_obRect) &&
                       pFrame.Texture.Name == m_pobTexture.Name &&
                       pFrame.Offset.equals(m_obUnflippedOffsetPositionFromCenter)
                   );
        }

        protected void UpdateBlendFunc()
        {
            // CCSprite: updateBlendFunc doesn't work when the sprite is rendered using a CCSpriteSheet
            Debug.Assert(m_pobBatchNode == null,
                         "CCSprite: updateBlendFunc doesn't work when the sprite is rendered using a CCSpriteSheet");

            // it's possible to have an untextured sprite
            if (m_pobTexture == null || !m_pobTexture.HasPremultipliedAlpha)
            {
                m_sBlendFunc.src = OGLES.GL_SRC_ALPHA;
                m_sBlendFunc.dst = OGLES.GL_ONE_MINUS_SRC_ALPHA;
                IsOpacityModifyRGB = false;
            }
            else
            {
                m_sBlendFunc.src = ccMacros.CC_BLEND_SRC;
                m_sBlendFunc.dst = ccMacros.CC_BLEND_DST;
                IsOpacityModifyRGB = true;
            }
        }
    }
}