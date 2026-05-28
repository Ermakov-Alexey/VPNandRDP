using DevExpress.XtraTab;
using DevExpress.XtraTab.ViewInfo;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using LogsFile;

namespace VaR
{
    public class EzyTabControl : XtraTabControl
    {
        public delegate void TabPageMovedDelegate(object sender, TabDragEventArgs e);

        [Description("Fires when a tabPage has been moved to a new location.")]
        public event TabPageMovedDelegate TabPageMoved;

        private bool _allowTabPageMove;
        private readonly Cursor _tabMoveCursor;

        private Rectangle _mRectDragBoxFromMouseDown;

        private XtraTabPage _originalTp;
        private int _originalTpLoc;
        private bool _mIsDragging;

        public EzyTabControl()
        {
            //InitializeComponent();
            try
            {
                _tabMoveCursor = Cursors.Default;
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
            }
        }

        /// <summary>
        /// Enables the tabPages to be re-ordered.
        /// </summary>
        [DefaultValue(false)]
        [Category("Behavior")]
        [Description("Enables the tabPages to be re-ordered.")]
        public bool AllowTabPageMove
        {
            get => _allowTabPageMove;
            set
            {
                _allowTabPageMove = value;
                if (value && !AllowDrop)
                    AllowDrop = true;
            }
        }


        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            CalcRectDragBox(e.X, e.Y);

            var hInfo = CalcHitInfo(new Point(e.X, e.Y));
            if (hInfo.Page != null)
            {
                //Log the original index and tabPage to restore if the user drags it outside the control////
                _originalTp = hInfo.Page;
                _originalTpLoc = FindTabIndex(_originalTp);
            }

        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            // 1. Быстрая проверка: если контрол удаляется или вкладок нет — выходим
            if (IsDisposed || Disposing || TabPages.Count == 0) return;

            try
            {
                // 2. Вызываем базовую реализацию (она может кинуть NullRef из-за десинхрона ViewInfo)
                base.OnMouseMove(e);
            } catch (NullReferenceException ex)
            {
                // 3. Ловим и логируем. Это безопасно: просто пропустим обновление hover-эффекта
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(),
                    "Пропущено внутреннее исключение DevExpress при обновлении hot-track (ViewInfo desync).", ex);
                return;
            }
            if (e.Button == MouseButtons.Left & _allowTabPageMove)
            {
                if (!_mRectDragBoxFromMouseDown.Equals(Rectangle.Empty) && !_mRectDragBoxFromMouseDown.Contains(e.X, e.Y))
                {
                    if (SelectedTabPage != null)
                    {
                        _mIsDragging = true;
                        DoDragDrop(SelectedTabPage, DragDropEffects.Move);
                        CalcRectDragBox(e.X, e.Y);
                    }
                }
            }

        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            base.OnDragDrop(drgevent);
            XtraTabPage dragTab = (XtraTabPage)drgevent.Data.GetData(typeof(XtraTabPage));
            int dropLocationIndex = FindTabIndex(dragTab);

            if (dropLocationIndex != _originalTpLoc)
            {
                TabPageMoved?.Invoke(this, new TabDragEventArgs(dragTab, dropLocationIndex));
            }


        }

        protected override void OnDragOver(DragEventArgs drgevent)
        {
            base.OnDragOver(drgevent);

            var hInfo = CalcHitInfo(PointToClient(new Point(drgevent.X, drgevent.Y)));

            if (hInfo.Page != null & _mIsDragging & _allowTabPageMove)
            {
                var hoverTab = hInfo.Page;

                if (drgevent.Data.GetDataPresent(typeof(XtraTabPage)))
                {
                    drgevent.Effect = DragDropEffects.Move;
                    var dragTab = (XtraTabPage)drgevent.Data.GetData(typeof(XtraTabPage));

                    int itemDragIndex = FindTabIndex(dragTab);
                    int dropLocationIndex = FindTabIndex(hoverTab);

                    if (itemDragIndex != dropLocationIndex && drgevent.AllowedEffect == DragDropEffects.Move)
                    {
                        MoveTabPages(dropLocationIndex, itemDragIndex, hInfo.HitPoint.X);
                    }
                }
                else
                    drgevent.Effect = DragDropEffects.None;
            }
            else
                drgevent.Effect = DragDropEffects.None;

        }

        private void MoveTabPages(int dropLocationIndex, int itemDragIndex, int ptX)
        {

            BaseTabControlViewInfo viewInfo = ((IXtraTab)this).ViewInfo;

            Rectangle selRect = viewInfo.HeaderInfo.AllPages[itemDragIndex].Bounds;
            Rectangle dropRect = viewInfo.HeaderInfo.AllPages[dropLocationIndex].Bounds;

            //Check the mouse position and do not swop the tabPages//
            //if it is not far enough over the target tabPage////////
            if (itemDragIndex < dropLocationIndex)
            {
                if (ptX < (selRect.Left + dropRect.Width))
                    return;
            }
            else
                if (ptX > (selRect.Right - dropRect.Width))
                return;

            var selTab = TabPages[itemDragIndex];
            var repTab = TabPages[dropLocationIndex];

            TabPages.Move(itemDragIndex, repTab);
            TabPages.Move(dropLocationIndex, selTab);
            SelectedTabPage = selTab;

        }

        protected override void OnDragLeave(EventArgs e)
        {
            base.OnDragLeave(e);
            int newIndex = FindTabIndex(_originalTp);
            if (newIndex != _originalTpLoc)
            {
                if (newIndex < _originalTpLoc)
                    TabPages.Move(_originalTpLoc + 1, _originalTp);
                else
                    TabPages.Move(_originalTpLoc, _originalTp);
            }
            _mIsDragging = false;
        }

        protected override void OnGiveFeedback(GiveFeedbackEventArgs gfbevent)
        {
            base.OnGiveFeedback(gfbevent);

            if (_tabMoveCursor == null)
                return;

            gfbevent.UseDefaultCursors = false;
            Cursor.Current = gfbevent.Effect == DragDropEffects.Move ? _tabMoveCursor : Cursors.No;
        }

        private void CalcRectDragBox(int x, int y)
        {
            Size dragSize = SystemInformation.DragSize;
            _mRectDragBoxFromMouseDown = new Rectangle(new Point(x - (dragSize.Width / 2), y - (dragSize.Height / 2)), dragSize);
        }

        private int FindTabIndex(XtraTabPage tPage)
        {
            //for(int i = 0; i < TabPages.Count; i++)
            //{
            //    if (TabPages[i].Equals(tPage))
            //    {
            //        return i;
            //    }
            //}

            return TabPages.IndexOf(tPage);
        }
    }

    public class TabDragEventArgs(XtraTabPage tabPage, int newIndex)
    {
        public XtraTabPage TabPage { get; set; } = tabPage;

        public int NewIndex { get; set; } = newIndex;
    }
}
