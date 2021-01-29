using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 車牌辨識
{
    public partial class Form1 : Form
    {
        FastPixel f = new FastPixel();//宣告快速繪圖物件
        int BlockBrightnessDimension = 10;//計算區域亮度區塊的寬與高
        int[,] BrightnessThreshold;//每一區塊的平均亮度，二值化門檻值
        int minHeight = 20;
        int maxHeight = 80;//有效目標高度範圍
        int minWidth = 2;
        int maxWidth = 80;//有效目標寬度範圍
        int Tgmax = 20;//進入決選範圍的最明顯目標上限
        byte[,] arrayGray;//灰階陣列
        byte[,] arrayBinary;//二值化陣列
        byte[,] arrayOutline;//輪廓線陣列
        ArrayList target_Collection,Atarget_Collection;//目標物件集合
        Bitmap basemap_Copy;//底圖副本
        Rectangle rec_Target;//收集目標範圍框
        TgInfo target_Processing;//擇處理之目標
        byte[,] arrayBinary_Processing;//擇處理之二值化陣列
        static int width_StandardFontImage=25;
        static int height_StandardFontImage=50;//標準字模影像之寬與高
        int width_StandardCharacterTarget = 0;
        int height_StandardCharacterTarget = 0;//標準字元目標寬高
        Array[] arrayBinary_NormalizationCompleted;//正規化完成後的字元二值化陣列
        double angle_LicencePlate_Inclination = 0;//車牌傾斜角度(>0為順時針傾斜)
        byte[,,,] array_License_Plate = new byte[2, 36, width_StandardFontImage, height_StandardFontImage];//六與七碼車牌所有英數字二值化陣列
        byte[,,] array_Deform_69 = new byte[2, width_StandardFontImage, height_StandardFontImage];//變形的六碼車牌6與9字型
        string License_Plate = "";//車牌號碼
        bool[] viewed_Target_Annotation;//已檢視目標註記
        //字元對照表
        //0-9→0-9
        //10→A，11→B，12→C，13→D，14→E，15→F，16→G，17→H，18→I，19→J
        //20→K，21→L，22→M，23→N，24→O，25→P，26→Q，27→R，28→S，29→T
        //30→U，31→V，32→W，33→X，34→Y，35→Z
        char[] Ch = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N','O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
        byte[,] V;//虛擬車牌二值化陣列



        public Form1()
        {
            InitializeComponent();
        }
        //開啟檔案
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(openFileDialog1.ShowDialog()==DialogResult.OK)
            {
                Bitmap bmp = new Bitmap(openFileDialog1.FileName);
                f.BMP2RGB(bmp);//讀取RGB亮度陣列
                arrayGray = f.arrayG;
                pictureBox1.Image = bmp;

            }
        }
        //以紅光為灰階
        private void redToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = f.GrayImg(f.arrayR);
        }
        //以綠光為灰階
        private void greenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = f.GrayImg(f.arrayG);
        }
        //以藍光為灰階
        private void blueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = f.GrayImg(f.arrayB);
        }
        //以RGB整合亮度為灰階
        private void rGBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byte[,] A = new byte[f.imagewidth, f.imageheight];
            for (int j=0;j<f.imageheight; j++)
            {
                for(int i = 0; i < f.imagewidth; i++)
                {
                    byte gray = (byte)(f.arrayR[i,j] * 0.299 + f.arrayG[i,j] * 0.587 + f.arrayB[i,j] * 0.114);
                    A[i, j] = gray;
                }
            }
            pictureBox1.Image = f.GrayImg(A);
        }
        //選擇紅綠光之較暗亮度為灰階
        private void rGLowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byte[,] A = new byte[f.imagewidth, f.imageheight];
            for (int j = 0; j < f.imageheight; j++)
            {
                for(int i = 0; i < f.imagewidth; i++)
                {
                    if(f.arrayR[i,j] > f.arrayG[i,j])
                    {
                        A[i, j] = f.arrayG[i, j];
                    }
                    else
                    {
                        A[i, j] = f.arrayR[i, j];
                    }
                }
            }
            pictureBox1.Image = f.GrayImg(A);
        }
        //二值化
        private void binaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BrightnessThreshold = ThresholdBuild(arrayGray);//門檻值陣列建立
            arrayBinary = new byte[f.imagewidth, f.imageheight];
            for(int i=1; i < f.imagewidth-1; i++)
            {
                int x = i / BlockBrightnessDimension;
                for(int j = 1; j < f.imageheight-1; j++)
                {
                    int y = j / BlockBrightnessDimension;
                    if (f.arrayG[i, j] < BrightnessThreshold[x,y])
                    {
                        arrayBinary[i, j] = 1;
                    }
                }
            }
            pictureBox1.Image = f.BWImg(arrayBinary);
        }
        //負片
        private void negativeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byte[,] A = new byte[f.imagewidth, f.imageheight];
            for(int j = 0; j < f.imageheight; j++)
            {
                for(int i = 0; i < f.imagewidth; i++)
                {
                    A[i, j] = (byte)(255 - f.arrayG[i, j]);
                }
            }
            pictureBox1.Image = f.GrayImg(A);
        }
        //儲存現狀影像
        private void saveImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog()==DialogResult.OK)
            {
                pictureBox1.Image.Save(saveFileDialog1.FileName);
            }
        }

        private void grayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = f.GrayImg(f.arrayG);
        }

        //平均亮度方塊圖
        private void aveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int kx = f.imagewidth / BlockBrightnessDimension;
            int ky = f.imageheight / BlockBrightnessDimension;
            BrightnessThreshold = new int[kx, ky];//區塊陣列
            //累計各區塊亮度值總和
            for(int i=0;i<f.imagewidth;i++)
            {
                int x = i / BlockBrightnessDimension;
                for(int j = 0; j < f.imageheight; j++)
                {
                    int y = j / BlockBrightnessDimension;
                    BrightnessThreshold[x,y] += f.arrayG[i, j];
                }
            }
            //建立亮度塊狀圖
            byte[,] A = new byte[f.imagewidth, f.imageheight];
            for(int i = 0; i < kx; i++) 
            {
                for(int j = 0; j < ky; j++)
                {
                    BrightnessThreshold[i, j] /= BlockBrightnessDimension*BlockBrightnessDimension;
                    for(int ii = 0; ii < BlockBrightnessDimension; ii++)
                    {
                        for(int jj = 0; jj < BlockBrightnessDimension; jj++)
                        {
                            A[i * BlockBrightnessDimension + ii, j * BlockBrightnessDimension + jj] = (byte)BrightnessThreshold[i, j];
                        }
                    }
                }
            }
            pictureBox1.Image = f.GrayImg(A);

        }

        //建立輪廓點陣列
        private byte[,] Outline(byte[,]b)
        {
            byte[,] arrayOutline = new byte[f.imagewidth, f.imageheight];
            for(int i = 1; i < f.imagewidth - 1; i++)
            {
                for(int j = 1; j < f.imageheight - 1; j++)
                {
                    if (b[i, j] == 0) continue;
                    if (b[i - 1, j] == 0) { arrayOutline[i, j] = 1;continue; }
                    if (b[i + 1, j] == 0) { arrayOutline[i, j] = 1;continue; }
                    if (b[i, j - 1] == 0) { arrayOutline[i, j] = 1;continue; }
                    if (b[i, j + 1] == 0) { arrayOutline[i, j] = 1; }
                }
            }
            return arrayOutline;
        }

        //輪廓線
        private void outlineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            arrayBinary = DoBinary(arrayGray);
            arrayOutline = Outline(arrayBinary);
            pictureBox1.Image = f.BWImg(arrayOutline);
        }
        //點選字元
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int m = e.X / (width_StandardFontImage + 4);
                if (m < 0 || m > arrayBinary_NormalizationCompleted.Length - 1) return;
                ChInfo C = BestC((byte[,])arrayBinary_NormalizationCompleted[m]);
                MessageBox.Show(C.Ch.ToString() + "," + C.fit_Score.ToString());
            }
        }

        //氾濫式演算法取得某目標之輪廓點
        private ArrayList getGrp(byte[,] q,int i,int j)
        {
            if (q[i, j] == 0) return new ArrayList();
            byte[,] b = (byte[,])q.Clone();//建立輪廓點陣列副本
            ArrayList nc = new ArrayList();//每一輪搜尋的起點集合
            nc.Add(new Point(i, j));//輸入之搜尋起點
            b[i, j] = 0;//清除此起點之輪廓點標記
            ArrayList A = nc;//此目標中所有目標點的集合
            do
            {
                ArrayList nb = (ArrayList)nc.Clone();//複製此輪之搜尋起點集合
                nc = new ArrayList();//清除準備蒐集下一輪搜尋起點之集合
                for (int m = 0; m < nb.Count; m++)
                {
                    Point p = (Point)nb[m];//搜尋起點
                    //在此點周邊3*3區域內找輪廓點
                    for (int ii = p.X - 1; ii <= p.X + 1; ii++)
                    {
                        for (int jj = p.Y - 1; jj <= p.Y + 1; jj++)
                        {
                            if (b[ii, jj] == 0) continue;//非輪廓點忽略
                            Point k = new Point(ii, jj);//建立點物件
                            nc.Add(k);//本輪搜尋新增的輪廓點
                            A.Add(k);//加入所有已蒐集到的目標點集合
                            b[ii, jj] = 0;//清除輪廓點點標記
                        }
                    }
                }
            } while (nc.Count > 0);//此輪搜尋有新發現輪廓點時繼續搜尋
            return A;//目標物件集合
        }

        //門檻值陣列建立
        private int[,] ThresholdBuild(byte[,]b)
        {
            int kx = f.imagewidth / BlockBrightnessDimension, ky = f.imageheight / BlockBrightnessDimension;
            BrightnessThreshold = new int[kx, ky];
            //累計各區塊亮度值總和
            for(int i=0;i<f.imagewidth;i++)
            {
                int x = i / BlockBrightnessDimension;
                for(int j=0;j<f.imageheight;j++)
                {
                    int y = j / BlockBrightnessDimension;
                    BrightnessThreshold[x, y] += f.arrayG[i, j];
                }
            }
            for(int i=0;i<kx;i++)
            {
                for(int j=0;j<ky;j++)
                {
                    BrightnessThreshold[i, j] /= BlockBrightnessDimension * BlockBrightnessDimension;
                }
            }
            return BrightnessThreshold;
        }

        //以輪廓點建立目標陣列，排除負目標
        private ArrayList getTargets(byte[,]q)
        {
            ArrayList A = new ArrayList();
            byte[,] b = (byte[,])q.Clone();//建立輪廓點陣列副本
            for(int i = 1; i < f.imagewidth - 1; i++)
            {
                for(int j = 1; j < f.imageheight - 1; j++)
                {
                    if (b[i, j] == 0) continue;
                    TgInfo G = new TgInfo();
                    G.x_max_negative = i;
                    G.x_max_positive = i;
                    G.y_max_negative = j;
                    G.y_max_positive = j;
                    G.targetPointList = new ArrayList();
                    ArrayList nc = new ArrayList();//每一輪搜尋的起點集合
                    nc.Add(new Point(i, j));//輸入之搜尋起點
                    G.targetPointList.Add(new Point(i, j));
                    b[i, j] = 0;//清除此起點之輪廓點標記
                    do
                    {
                        ArrayList nb = (ArrayList)nc.Clone();//複製此輪之搜尋點集合
                        nc = new ArrayList();//清除準備蒐集下一輪搜尋起點之集合
                        for (int m = 0; m < nb.Count; m++)
                        {
                            Point p = (Point)nb[m];//搜尋起點
                            //在此點周邊3*3區域內找輪廓點
                            for (int ii = p.X - 1; ii <= p.X + 1; ii++)
                            {
                                for (int jj = p.Y - 1; jj <= p.Y + 1; jj++)
                                {
                                    if (b[ii, jj] == 0) continue;//非輪廓點忽略
                                    Point k = new Point(ii, jj);//建立點物件
                                    nc.Add(k);//本輪搜尋新增的輪廓點
                                    G.targetPointList.Add(k);//點集合
                                    G.targetPoint += 1;//點數累計
                                    if (ii < G.x_max_negative) G.x_max_negative = ii;
                                    if (ii > G.x_max_positive) G.x_max_positive = ii;
                                    if (jj < G.y_max_negative) G.y_max_negative = jj;
                                    if (jj > G.y_max_positive) G.y_max_positive = jj;
                                    b[ii, jj] = 0;//清除輪廓點點標記
                                }
                            }
                        }
                    } while (nc.Count > 0);//此輪搜尋有新發現輪廓點時繼續搜尋
                    if (arrayBinary[i - 1, j] == 1) continue;//排除白色區塊的負目標，起點左邊是黑點
                    G.width = G.x_max_positive - G.x_max_negative + 1;//寬度計算
                    G.height = G.y_max_positive - G.y_max_negative + 1;//高度計算
                    //以寬高大小篩選目標
                    if (G.height < minHeight) continue;
                    if (G.height > maxHeight) continue;
                    if (G.width < minWidth) continue;
                    if (G.width > maxWidth) continue;
                    G.x_target = (G.x_max_negative + G.x_max_positive) / 2;G.y_target = (G.y_max_negative + G.y_max_positive) / 2;//中心點
                    //計算目標的對比度
                    for(int m = 0; m < G.targetPointList.Count; m++)
                    {
                        int pm = PointPm((Point)G.targetPointList[m]);
                        if (pm > G.contrast_target_back) G.contrast_target_back = pm;//最高對比度的輪廓點
                    }
                    A.Add(G);//加入有效目標集合
                }
            }
            //以對比度排序
            for(int i = 0; i <= Tgmax; i++)
            {
                if (i > A.Count - 1) break;
                for(int j = i + 1; j < A.Count; j++)
                {
                    TgInfo T = (TgInfo)A[i], G = (TgInfo)A[j];
                    if(T.contrast_target_back<G.contrast_target_back)//互換位置，高對比目標在前
                    {
                        A[i] = G;
                        A[j] = T;
                    }
                }
            }
            //取得Tgmax個最明顯的目標輸出
            target_Collection = new ArrayList();
            for(int i = 0; i < Tgmax; i++)
            {
                if (i > A.Count - 1) break;//超過總目標數
                TgInfo T = (TgInfo)A[i];T.ID_contrast = i;//建立以對比度排序的序號
                target_Collection.Add(T);
            }
            return target_Collection;//回傳目標物件集合
        }

        //建立合格目標物件集合
        private void targetsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            target_Collection = getTargets(arrayOutline);//建立目標物件集合
            //繪製有效目標輪廓點
            Bitmap bmp = new Bitmap(f.imagewidth, f.imageheight);
            for(int k = 0; k <= 10; k++)
            {
                TgInfo T = (TgInfo)target_Collection[k];
                for(int m = 0; m < T.targetPointList.Count; m++)
                {
                    Point p = (Point)T.targetPointList[m];
                    bmp.SetPixel(p.X, p.Y, Color.Black);
                }
            }
            pictureBox1.Image = bmp;
            basemap_Copy = (Bitmap)bmp.Clone();
        }

        //依據目標大小篩選目標
        private void filterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ArrayList D = new ArrayList();
            for(int k = 0; k < target_Collection.Count; k++)
            {
                TgInfo T = (TgInfo)target_Collection[k];
                if (T.height < minHeight) continue;
                if (T.height > maxHeight) continue;
                if (T.width < minWidth) continue;
                if (T.width > maxWidth) continue;
                D.Add(T);
            }
            target_Collection = D;
            //繪製有效目標
            Bitmap bmp = new Bitmap(f.imagewidth, f.imageheight);
            for(int k = 0; k < target_Collection.Count - 1; k++)
            {
                TgInfo T = (TgInfo)target_Collection[k];
                for(int m = 0; m < T.targetPointList.Count; m++)
                {
                    Point p = (Point)T.targetPointList[m];
                    bmp.SetPixel(p.X, p.Y, Color.Black);
                }
            }
            pictureBox1.Image = bmp;
            basemap_Copy = (Bitmap)bmp.Clone();
        }

        //輪廓點與背景的對比度
        private int PointPm(Point p)
        {
            int x = p.X, y = p.Y;
            int mx = arrayGray[x, y];
            if (mx < arrayGray[x - 1, y]) mx = arrayGray[x - 1, y];
            if (mx < arrayGray[x + 1, y]) mx = arrayGray[x + 1, y];
            if (mx < arrayGray[x, y - 1]) mx = arrayGray[x, y - 1];
            if (mx < arrayGray[x, y + 1]) mx = arrayGray[x, y + 1];
            return mx - arrayGray[x, y];
        }

        //依據對比度排序前10大目標
        private void sortToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //建立目標物件的對比度屬性
            for(int k = 0; k < target_Collection.Count; k++)
            {
                TgInfo T = (TgInfo)target_Collection[k];
                for(int m = 0; m < T.targetPointList.Count; m++)
                {
                    int pm = PointPm((Point)T.targetPointList[m]);
                    if (pm > T.contrast_target_back) T.contrast_target_back = pm;
                }
                target_Collection[k] = T;
            }
            //依對比度排序
            for(int i = 0; i < 10; i++)
            {
                for(int j = i + 1; j < target_Collection.Count; j++)
                {
                    TgInfo T = (TgInfo)target_Collection[i], G = (TgInfo)target_Collection[j];
                    if(T.contrast_target_back<G.contrast_target_back)
                    {
                        target_Collection[i] = G;
                        target_Collection[j] = T;
                    }
                }
            }
            //繪製有效目標
            Bitmap bmp = new Bitmap(f.imagewidth, f.imageheight);
            for(int k = 0; k < 10; k++)
            {
                TgInfo T = (TgInfo)target_Collection[k];
                for(int m = 0; m < T.targetPointList.Count; m++)
                {
                    Point p = (Point)T.targetPointList[m];
                    bmp.SetPixel(p.X, p.Y, Color.Black);
                }
            }
            pictureBox1.Image = bmp;
            basemap_Copy = (Bitmap)bmp.Clone();
        }

        //二值化
        private byte[,]DoBinary(byte[,]b)
        {
            BrightnessThreshold = ThresholdBuild(b);
            arrayBinary = new byte[f.imagewidth, f.imageheight];
            for(int i = 1; i < f.imagewidth - 1; i++)
            {
                int x = i / BlockBrightnessDimension;
                for(int j = 1; j < f.imageheight - 1; j++)
                {
                    int y = j / BlockBrightnessDimension;
                    if (f.arrayG[i, j] < BrightnessThreshold[x, y])
                    {
                        arrayBinary[i, j] = 1;
                    }
                }
            }
            return arrayBinary;
        }

        //找車牌字元目標群組
        private ArrayList AlignTgs(ArrayList C)
        {
            ArrayList R = new ArrayList();
            int pmx = 0;//最佳目標組合與最佳對比度
            for(int i = 0; i < C.Count; i++)
            {
                TgInfo T = (TgInfo)C[i];//核心目標
                ArrayList D = new ArrayList();
                int Dm = 0;//此輪搜尋的目標集合
                D.Add(T);
                Dm = T.contrast_target_back;//加入搜尋起點目標
                //搜尋X範圍
                int x1 = (int)(T.x_target - T.height * 2.5);
                int x2 = (int)(T.x_target + T.height * 2.5);
                //搜尋Y範圍
                int y1 = (int)(T.y_target - T.height * 1.5);
                int y2 = (int)(T.y_target + T.height * 1.5);
                for(int j=0;j<C.Count;j++)
                {
                    if (i == j) continue;//與起點重複略過
                    TgInfo G = (TgInfo)C[j];
                    if (G.x_target < x1) continue;
                    if (G.x_target > x2) continue;
                    if (G.y_target < y1) continue;
                    if (G.y_target > y2) continue;
                    if (G.width > T.height) continue;//目標寬度太大略過
                    if (G.height > T.height * 1.5) continue;//目標高度太大略過
                    D.Add(G);Dm += G.contrast_target_back;//合格目標加入集合
                    if (D.Count >= 7) break;//目標蒐集個數已滿跳離迴圈
                }
                if(Dm>pmx)//對比度高於之前的目標集合
                {
                    pmx = Dm;
                    R = D;
                }
            }
            //目標群位置左右排序
            if (R.Count > 1)
            {
                int n = R.Count;
                for(int i = 0; i < n - 1; i++)
                {
                    for(int j = i + 1; j < n; j++)
                    {
                        TgInfo Ti = (TgInfo)R[i];
                        TgInfo Tj = (TgInfo)R[j];
                        if (Ti.x_target > Tj.x_target)
                        {
                            R[i] = Tj;
                            R[j] = Ti;
                        }
                    }
                }
            }
            return R;
        }

        //找車牌字元目標群組
        private void alignToolStripMenuItem_Click(object sender, EventArgs e)
        {
            arrayBinary = DoBinary(arrayGray);//二值化
            arrayOutline = Outline(arrayBinary);//建立輪廓點陣列
            target_Collection = getTargets(arrayOutline);//建立目標物件集合
            target_Collection = AlignTgs(target_Collection);//找到最多七個的字元目標群組
            //末字中心點與首字中心點的偏移量，斜率計算參數
            int n = target_Collection.Count;
            int dx = ((TgInfo)target_Collection[n - 1]).x_target - ((TgInfo)target_Collection[0]).x_target;
            int dy = ((TgInfo)target_Collection[n - 1]).y_target - ((TgInfo)target_Collection[0]).y_target;
            angle_LicencePlate_Inclination = Math.Atan2((double)dy, (double)dx);//字元排列傾角
            //繪製有效目標
            Bitmap bmp = new Bitmap(f.imagewidth, f.imageheight);
            for(int k = 0; k < target_Collection.Count; k++)
            {
                TgInfo T = (TgInfo)target_Collection[k];
                for(int m = 0; m < T.targetPointList.Count; m++)
                {
                    Point p = (Point)T.targetPointList[m];
                    bmp.SetPixel(p.X, p.Y, Color.Black);
                }
            }
            pictureBox1.Image = bmp;
        }

        //旋轉目標
        private void rotateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            arrayBinary_Processing = RotateTg(arrayBinary_Processing, ref target_Processing, angle_LicencePlate_Inclination);//旋轉目標二值化影像
            pictureBox1.Image = f.BWImg(arrayBinary_Processing);//繪製轉正後之影像
        }
        //將單一目標轉正
        private byte[,] RotateTg(byte[,]b,ref TgInfo T,double A)
        {
            if (A == 0) return b;//無傾斜不須旋轉
            if (A > 0) A = -A;//順或逆時針傾斜時需要旋轉方向相反，經過推導A應該永遠為負值
            double[,] R = new double[2, 2];//旋轉矩陣
            R[0, 0] = Math.Cos(A);
            R[0, 1] = Math.Sin(A);
            R[1, 0] = -R[0, 1];
            R[1, 1] = R[0, 0];
            int x0 = T.x_max_negative;
            int y0 = T.y_max_positive;//左下角座標
            //旋轉後之目標範圍
            int xmn = f.imagewidth;
            int xmx = 0;
            int ymn = f.imageheight;
            int ymx = 0;
            for(int i = T.x_max_negative; i <= T.x_max_positive; i++)
            {
                for(int j = T.y_max_negative; j <= T.y_max_positive; j++)
                {
                    if (b[i, j] == 0) continue;//空點無須旋轉
                    int x = i - x0;
                    int y = y0 - j;//轉換螢幕座標為直角座標
                    int xx = (int)(x * R[0, 0] + y * R[0, 1] + x0);//旋轉後x座標
                    if (xx < 1 || xx > f.imagewidth - 2) continue;//邊界淨空
                    int yy = (int)(y0 - (x * R[1, 0] + y * R[1, 1]));//旋轉後y座標
                    if (yy < 1 || yy > f.imageheight - 2) continue;//邊界淨空
                    b[i, j] = 0;
                    b[xx, yy] = 1;
                    //旋轉後目標的範圍偵測
                    if (xx < xmn) xmn = xx;
                    if (xx > xmx) xmx = xx;
                    if (yy < ymn) ymn = yy;
                    if (yy > ymx) ymx = yy;
                }
            }
            //重設目標屬性
            T.x_max_negative = xmn;
            T.x_max_positive = xmx;
            T.y_max_negative = ymn;
            T.y_max_positive = ymx;
            T.width = T.x_max_positive - T.x_max_negative + 1;
            T.height = T.y_max_positive - T.y_max_negative + 1;
            T.x_target = (T.x_max_positive + T.x_max_negative) / 2;
            T.y_target = (T.y_max_positive + T.y_max_negative) / 2;
            //補足因為旋轉運算時產生的數位化誤差造成的資料空點
            for(int i = T.x_max_negative; i <= T.x_max_positive; i++)
            {
                for(int j = T.y_max_negative; j <= T.y_max_positive; j++)
                {
                    if (b[i, j] == 1) continue;
                    if (b[i - 1, j] + b[i + 1, j] + b[i, j - 1] + b[i, j + 1] >= 3)
                    {
                        b[i, j] = 1;
                    }
                }
            }
            return b;
        }

        //字元目標正規化到字模寬高
        private void normalizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double fx = (double)target_Processing.width / width_StandardFontImage;
            double fy = (double)target_Processing.height / height_StandardFontImage;
            byte[,] V = new byte[width_StandardFontImage, height_StandardFontImage];
            for(int i = 0; i < width_StandardFontImage; i++)
            {
                int x = target_Processing.x_max_negative + (int)(i * fx);
                for(int j = 0; j < height_StandardFontImage; j++)
                {
                    int y = target_Processing.y_max_negative + (int)(j * fy);
                    V[i, j] = arrayBinary_Processing[x, y];
                }
            }
            pictureBox1.Image = f.BWImg(V);
        }

        //完整辨識處理
        private void correctAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = target_Collection.Count;//目標總數
            //旋轉所有目標
            TgInfo[] T = new TgInfo[n];
            Array[] M = new Array[n];
            int[] w = new int[n];
            int[] h = new int[n];
            for(int k = 0; k < n; k++)
            {
                TgInfo G = (TgInfo)target_Collection[k];
                M[k] = Tg2Bin(G);//建立單一目標的二值化矩陣
                TgInfo GG = new TgInfo().Clone(G);
                M[k] = RotateTg((byte[,])M[k], ref GG, angle_LicencePlate_Inclination);//旋轉目標
                T[k] = GG;//儲存旋轉後的目標物件
                w[k] = GG.width;//寬度陣列
                h[k] = GG.height;//高度陣列
            }
            Array.Sort(w);
            Array.Sort(h);//寬高度排序，小到大
            width_StandardCharacterTarget = w[n - 2];
            height_StandardCharacterTarget = h[n - 2];//取第二寬或高的目標為標準，避開意外沾黏的極端目標
            //車牌全圖矩陣，字元間隔4畫素
            byte[,] R = new byte[(width_StandardFontImage + 4) * n, height_StandardFontImage];
            arrayBinary_NormalizationCompleted = new Array[n];
            for(int k = 0; k < n; k++)
            {
                arrayBinary_NormalizationCompleted[k] = NmBin(T[k], (byte[,])M[k], width_StandardCharacterTarget, height_StandardCharacterTarget);//個別字元正規化矩陣
                int xs = (width_StandardFontImage + 4) * k;//X偏移量
                for(int i = 0; i < width_StandardFontImage; i++)
                {
                    for(int j = 0; j < height_StandardFontImage; j++)
                    {
                        R[xs + i, j] = ((byte[,])arrayBinary_NormalizationCompleted[k])[i, j];
                    }
                }
            }
            pictureBox1.Image = f.BWImg(R);//顯示正規化之後的車牌
        }

        //建立單一目標的二值化矩陣
        private byte[,] Tg2Bin(TgInfo T)
        {
            byte[,] b = new byte[f.imagewidth, f.imageheight];//二值化陣列
            for(int n = 0; n < T.targetPointList.Count; n++)
            {
                Point p = (Point)T.targetPointList[n];
                b[p.X, p.Y] = 1;//起點
                //向右連通成實心影像
                int i = p.X + 1;
                while (arrayBinary[i, p.Y] == 1)
                {
                    b[i, p.Y] = 1;
                    i += 1;
                }
                //向左連通成實心影像
                i = p.X - 1;
                while (arrayBinary[i, p.Y] == 1)
                {
                    b[i, p.Y] = 1;
                    i -= 1;
                }
            }
            return b;
        }

        //建立正規化目標二值化陣列
        private byte[,] NmBin(TgInfo T,byte[,] M,int mw,int mh)
        {
            double fx = (double)mw / width_StandardFontImage;
            double fy = (double)mh / height_StandardFontImage;
            byte[,] V = new byte[width_StandardFontImage, height_StandardFontImage];
            for(int i = 0; i < width_StandardFontImage; i++)
            {
                int sx = 0;//過窄字元的平移量，預設不平移
                if(T.width/mw<0.75)//過窄字元，可能為1或I
                {
                    sx = (mw - T.width) / 2;//平移寬度差之一半
                }
                int x = (int)(T.x_max_negative + i * fx - sx);
                if (x < 0 || x > f.imagewidth - 1) continue;
                for(int j = 0; j < height_StandardFontImage; j++)
                {
                    int y = T.y_max_negative + (int)(j * fy);
                    V[i, j] = M[x, y];
                }
            }
            return V;
        }

        //加隔線
        private void addDashToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //計算最大字元間距
            int n = target_Collection.Count;
            int dmx = 0;
            int mi = 0;
            for(int i = 0; i < n - 1; i++)
            {
                int d1 = ((TgInfo)target_Collection[i + 1]).x_target - ((TgInfo)target_Collection[i]).x_target;
                int d2 = ((TgInfo)target_Collection[i + 1]).y_target - ((TgInfo)target_Collection[i]).y_target;
                int d = (d1 * d1) + (d2 * d2);
                if (d > dmx)
                {
                    dmx = d;
                    mi = i;
                }
            }
            //繪製含隔線車牌
            //車牌全圖矩陣，字元間隔4畫素
            byte[,] R = new byte[(width_StandardFontImage + 4) * n + 20, height_StandardFontImage];
            for(int k = 0; k < n; k++)
            {
                int xs = (width_StandardFontImage + 4) * k;
                if (k > mi) xs += 20;
                for(int i = 0; i < width_StandardFontImage; i++)
                {
                    for(int j = 0; j < height_StandardFontImage; j++)
                    {
                        R[xs + i, j] = ((byte[,])arrayBinary_NormalizationCompleted[k])[i, j];
                    }
                }
                if (k == mi)//繪製隔線
                {
                    xs += width_StandardFontImage + 2;
                    for(int i = 5; i < 15; i++)
                    {
                        for(int j = 23; j < 27; j++)
                        {
                            R[xs + i, j] = 1;
                        }
                    }
                }
            }
            pictureBox1.Image = f.BWImg(R);
        }

        //啟動程式載入字模
        private void Form1_Load(object sender, EventArgs e)
        {
            FontLoad();
        }

        //載入字模
        private void FontLoad()
        {
            byte[] q = 車牌辨識.Properties.Resources.font;//使用字模資源檔
            int n = 0;
            //匯入六與七碼字模
            for (int m = 0; m < 2; m++)
            {
                for (int k = 0; k < 36; k++)
                {
                    for (int j = 0; j < height_StandardFontImage; j++)
                    {
                        for (int i = 0; i < width_StandardFontImage; i++)
                        {
                            array_License_Plate[m, k, i, j] = q[n]; 
                            n += 1;
                        }
                    }
                }
            }
            //匯入六碼變形69字模
            for (int k = 0; k < 2; k++)
            {
                for (int j = 0; j < height_StandardFontImage; j++)
                {
                    for (int i = 0; i < width_StandardFontImage; i++)
                    {
                        array_Deform_69[k, i, j] = q[n]; 
                        n += 1;
                    }
                }
            }
        }

        //最佳字元
        private ChInfo BestC(byte[,] A)
        {
            ChInfo C = new ChInfo();
            //六七碼正常字形比對
            for(int m = 0; m < 2; m++)
            {
                for(int k = 0; k < 36; k++)
                {
                    int n0 = 0;//字模黑點數
                    int nf = 0;//符合的黑點數
                    for(int i = 0; i < width_StandardFontImage; i++)
                    {
                        for(int j = 0; j < height_StandardFontImage; j++)
                        {
                            if (array_License_Plate[m, k, i, j] == 0)
                            {
                                if (A[i, j] == 1)
                                {
                                    nf -= 1;//目標與字模不符合點數
                                }                             
                            }
                            else
                            {
                                n0 += 1;//字模黑點數累計
                                if (A[i, j] == 1)
                                {
                                    nf += 1;//目標與字模符合點數
                                }
                            }
                        }                       
                    }
                    int v = nf * 1000 / n0;//符合點數千分比
                    if (v > C.fit_Score)
                    {
                        C.fit_Score = v;
                        C.Ch = Ch[k];
                        C.kind_6or7 = m;
                    }
                }              
            }
            //變形6與9比對
            for (int k = 0; k < 2; k++)
            {
                int n0 = 0;//字模黑點數
                int nf = 0;//符合的黑點數
                for (int i = 0; i < width_StandardFontImage; i++)
                {
                    for (int j = 0; j < height_StandardFontImage; j++)
                    {
                        if (array_Deform_69[k, i, j] == 0)
                        {
                            if (A[i, j] == 1)
                            {
                                nf -= 1;//目標與字模不符合點數
                            }
                        }
                        else
                        {
                            n0 += 1;//字模黑點數累計
                            if (A[i, j] == 1)
                            {
                                nf += 1;//目標與字模符合點數
                            }
                        }
                    }
                }
                int v = nf * 1000 / n0;//符合點數千分比
                if (v > C.fit_Score)
                {
                    C.fit_Score = v;
                    if (k == 0)
                    {
                        C.Ch = '6';
                    }
                    else
                    {
                        C.Ch = '9';
                    }
                }
            }
            return C;
        }

        //辨識整個車牌
        private void recognizeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //計算最大字元間距與位置
            int n=target_Collection.Count;
            int dmx=0;
            int mi=0;
            for(int i=0;i<n-1;i++)
            {
                int d1=((TgInfo)target_Collection[i+1]).x_target-((TgInfo)target_Collection[i]).x_target;
                int d2=((TgInfo)target_Collection[i+1]).y_target-((TgInfo)target_Collection[i]).y_target;
                int d=(d1*d1)+(d2*d2);
                if(d>dmx)
                {
                    dmx=d;
                    mi=i;
                }
            }
            License_Plate="";//車牌字串
            int sc=0;//符合度
            for(int i=0;i<arrayBinary_NormalizationCompleted.Length;i++)
            {
                ChInfo k=BestC((byte[,])arrayBinary_NormalizationCompleted[i]);
                License_Plate+=k.Ch;
                sc+=k.fit_Score;
                if(i==mi)
                {
                    License_Plate+='-';
                }
            }
            sc/=arrayBinary_NormalizationCompleted.Length;
            MessageBox.Show(License_Plate+","+sc.ToString());
        }

        //檢驗車牌是否正確的程式
        private LPInfo ChkLP(LPInfo R)
        {
            if(R.Score<600)return new LPInfo();//符合度低於及格分數
            if(R.license_plate_number.Length<5)return new LPInfo();//包含分隔線在內字數小於5
            int m=R.license_plate_number.IndexOf('-');//格線位置
            if(m==1)return new LPInfo();//沒有1-x的字數區段格式
            return R;//合格車牌
        }

        //嘗試依據英數字規範修改車牌答案
        private LPInfo ChkED(LPInfo R)
        {
            if (R.license_plate_number == null) return R;
            char[] C=R.license_plate_number.ToCharArray();//字串轉成字元陣列
            int n1=R.license_plate_number.IndexOf('-');//第一區段長度
            int n2 = C.Length - n1 - 1;//第二區段長度
            int d1 = 0;
            int d2 = 0;//數字區的起終點
            if (n1 > n2) { d1 = 0; d2 = n1 - 1; }//第一區段較長
            if (n2 > n1) { d1 = n1 + 1;d2 = C.Length - 1; }//第二區段較長
            if (d2 == 0) return R;//無法判定純數字區段(2-2或3-3)
            //嘗試將純數字區段的英文字改成數字
            for(int i = d1; i <= d2; i++)
            {
                C[i] = E2D(C[i]);
            }
            //如果是七碼車牌，強制將前三碼中的數字改成英文
            if (n1 == 3 && n2 == 4)
            {
                for(int i = 0; i <= 2; i++)
                {
                    C[i] = D2E(C[i]);
                }
            }
            //重組字串
            R.license_plate_number = "";
            for(int i = 0; i < C.Length; i++)
            {
                R.license_plate_number += C[i];
            }
            return R;
        }
        //嘗試將英文字母變成相似的數字
        private char E2D(char C)
        {
            if (C == 'B') return '8';
            if (C == 'D') return '0';
            if (C == 'O') return '0';
            return C;
        }

        //嘗試將數字變成相似的英文字母
        private char D2E(char C)
        {
            if (C == '8') return 'B';
            if (C == '0') return 'D';
            return C;
        }

        //辨識整個車牌
        private void recognizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            arrayBinary = DoBinary(arrayGray);//二值化
            arrayOutline = Outline(arrayBinary);//建立輪廓點陣列
            Atarget_Collection = getTargets(arrayOutline);//建立所有目標物件集合
            viewed_Target_Annotation = new bool[Atarget_Collection.Count];
            LPInfo R = new LPInfo();
            //目標搜尋辨識
            for(int k = 0; k < 3; k++)
            {
                R = getLP(Atarget_Collection);
                if (R.Score > 0) break;//有合理答案立即跳出迴圈
            }
            //負片辨識
            if (R.Score == 0)
            {
                arrayGray = Negative(arrayGray);
                arrayBinary = DoBinary(arrayGray);//二值化
                arrayOutline = Outline(arrayBinary);//建立輪廓點陣列
                Atarget_Collection = getTargets(arrayOutline);//建立所有目標物件集合
                viewed_Target_Annotation = new bool[Atarget_Collection.Count];
                for(int k = 0; k < 3; k++)
                {
                    R = getLP(Atarget_Collection);
                    if (R.Score > 0) break;//有合理答案立即跳出迴圈
                }
            }
            this.Text = R.license_plate_number + "," + R.Score.ToString() + "," + R.cx.ToString() + "," + R.cy.ToString();
        }

        //左方外插一字
        private void leftExtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //字元間的最小間距
            int xd = f.imagewidth;
            int yd = f.imageheight;
            for(int i = 0; i < target_Collection.Count - 1; i++)
            {
                int dx = ((TgInfo)target_Collection[i + 1]).x_target - ((TgInfo)target_Collection[i]).x_target;
                if (dx < xd) xd = dx;
                int dy = ((TgInfo)target_Collection[i + 1]).y_target - ((TgInfo)target_Collection[i]).y_target;
                if (dy < yd) yd = dy;
            }
            TgInfo G = (TgInfo)target_Collection[0];//複製一個外插的目標
            //目標位置移動
            G.x_target -= xd;
            G.y_target -= yd;
            G.x_max_negative = G.x_target - width_StandardCharacterTarget / 2;
            G.x_max_positive = G.x_target + width_StandardCharacterTarget / 2;
            G.y_max_negative = G.y_target - height_StandardCharacterTarget / 2;
            G.y_max_positive = G.y_target + height_StandardCharacterTarget / 2;
            //外插目標二值化陣列
            byte[,] A = new byte[f.imagewidth, f.imageheight];
            for(int i = G.x_max_negative; i <= G.x_max_positive; i++)
            {
                for(int j = G.y_max_negative; j <= G.y_max_positive; j++)
                {
                    A[i, j] = arrayBinary[i, j];
                }
            }
            A = RotateTg(A, ref G, angle_LicencePlate_Inclination);//旋轉目標
            byte[,] D = NmBin(G, A, width_StandardCharacterTarget, height_StandardCharacterTarget);//外插字元正規化
            ChInfo k = BestC(D);//辨識字元
            License_Plate = k.Ch + License_Plate;//外插字元加入車牌
            //繪圖顯示外插目標框線
            Bitmap bmp = new Bitmap(openFileDialog1.FileName);
            Rectangle rec = new Rectangle(G.x_max_negative, G.y_max_negative, width_StandardCharacterTarget, height_StandardCharacterTarget);
            Graphics Gr = Graphics.FromImage(bmp);
            Gr.DrawRectangle(Pens.Red, rec);
            pictureBox1.Image = bmp;
            MessageBox.Show(License_Plate);
        }

        //灰階
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked && arrayGray!=null)
            {
                pictureBox1.Image = f.GrayImg(arrayGray);
            }
        }
        //二值化圖
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked && arrayBinary!=null)
            {
                pictureBox1.Image = f.BWImg(arrayBinary);
            }
        }
        //輪廓線
        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked && arrayOutline!=null)
            {
                pictureBox1.Image = f.BWImg(arrayOutline);
            }
        }
        //所有合格目標
        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
            {
                if (Atarget_Collection == null) return;
                Bitmap bmp = new Bitmap(f.imagewidth, f.imageheight);
                for(int k = 0; k < Atarget_Collection.Count; k++)
                {
                    TgInfo T = (TgInfo)Atarget_Collection[k];
                    for(int m = 0; m < T.targetPointList.Count; m++)
                    {
                        Point p = (Point)T.targetPointList[m];
                        bmp.SetPixel(p.X, p.Y, Color.Black);
                    }
                }
                pictureBox1.Image = bmp;
            }
        }

        //依據可能目標組合辨識車牌
        private LPInfo getLP(ArrayList A)
        {
            LPInfo R = new LPInfo();//建立車牌資訊物件
            target_Collection = AlignTgs(A);//找到最多七個的字元目標組合
            int n = target_Collection.Count;//目標個數
            for(int i = 0; i < n; i++)
            {
                viewed_Target_Annotation[i] = true;//標示目標已處理
            }
            if (n < 4) return R;//目標數目不足以構成車牌
            R.N_target = n;//車牌目標個數
            //末字中心點與首字中心點的偏移量，斜率計算參數
            int dx = ((TgInfo)target_Collection[n - 1]).x_target - ((TgInfo)target_Collection[0]).x_target;
            int dy = ((TgInfo)target_Collection[n - 1]).y_target - ((TgInfo)target_Collection[0]).y_target;
            angle_LicencePlate_Inclination = Math.Atan2((double)dy, (double)dx);//字元排列傾角
            //旋轉所有目標
            TgInfo[] T = new TgInfo[n];
            Array[] M = new Array[n];
            int[] w = new int[n];
            int[] h = new int[n];
            R.xmn = f.imagewidth;
            R.xmx = 0;
            R.ymn = f.imageheight;
            R.ymx = 0;//車牌四面極值
            for(int k = 0; k < n; k++)
            {
                TgInfo G = (TgInfo)target_Collection[k];
                M[k] = Tg2Bin(G);//建立單一目標的二值化矩陣
                M[k] = RotateTg((byte[,])M[k], ref G, angle_LicencePlate_Inclination);//旋轉目標
                T[k] = G;//儲存旋轉後的目標物件
                w[k] = G.width;//寬度陣列
                h[k] = G.height;//高度陣列
                if (G.x_max_negative < R.xmn) R.xmn = G.x_max_negative;
                if (G.x_max_positive > R.xmx) R.xmx = G.x_max_positive;
                if (G.y_max_negative < R.ymn) R.ymn = G.y_max_negative;
                if (G.y_max_positive > R.ymx) R.ymx = G.y_max_positive;
            }
            R.width = R.xmx - R.xmn + 1;
            R.height = R.ymx - R.ymn + 1;//車牌寬高
            R.cx = (R.xmn + R.xmx) / 2;
            R.cy = (R.ymn + R.ymx) / 2;//車牌中心點
            Array.Sort(w);
            Array.Sort(h);//寬高度排序，小到大
            int mw = w[n - 2];
            int mh = h[n - 2];//取第二寬或高的目標為標準，避開意外沾連的極端目標
            //目標正規化→寬高符合字模
            arrayBinary_NormalizationCompleted = new Array[n];//正規化後之字元二值化陣列
            for(int k = 0; k < n; k++)
            {
                arrayBinary_NormalizationCompleted[k] = NmBin(T[k], (byte[,])M[k], mw, mh);
            }
            //計算最大字元間距與位置
            int dmx = 0;
            int mi = 0;
            for(int i = 0; i < n - 1; i++)
            {
                int d1 = ((TgInfo)target_Collection[i + 1]).x_target - ((TgInfo)target_Collection[i]).x_target;
                int d2 = ((TgInfo)target_Collection[i + 1]).y_target - ((TgInfo)target_Collection[i]).y_target;
                int d = d1 ^ 2 + d2 ^ 2;
                if (d > dmx)
                {
                    dmx = d;
                    mi = i;
                }
            }
            //車牌虛擬矩陣，字元間隔2畫素
            int wd = width_StandardFontImage + (width_StandardFontImage + 2) * n + width_StandardFontImage;
            V = new byte[wd, height_StandardFontImage];
            for(int k = 0; k < n; k++)
            {
                int xs = width_StandardFontImage + (width_StandardFontImage + 2) * k;//X偏移量
                for(int i = 0; i < width_StandardFontImage; i++)
                {
                    for(int j = 0; j < height_StandardFontImage; j++)
                    {
                        V[xs + i, j] = ((byte[,])arrayBinary_NormalizationCompleted[k])[i, j];
                    }
                }
            }
            IncCorrect(V);//字元傾倒偵測校正
            for(int i = 0; i < arrayBinary_NormalizationCompleted.Length; i++)
            {
                ChInfo k = BestC((byte[,])arrayBinary_NormalizationCompleted[i]);
                R.license_plate_number += k.Ch;//車牌字串累加
                R.Score += k.fit_Score;//字源符合度累加
                if (i == mi) R.license_plate_number += '-';
            }
            R.Score /= arrayBinary_NormalizationCompleted.Length;
            R = ChkLP(R);//檢查是否為合格車牌
            R = ChkED(R);//修正英數字
            return R;//回傳車牌資料
        }

        //車牌
        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked)
            {
                if (V == null) return;
                pictureBox1.Image = f.BWImg(V);
            }
        }

        //負片
        private byte[,] Negative(byte[,] b)
        {
            for(int i = 0; i < f.imagewidth; i++)
            {
                for(int j = 0; j < f.imageheight; j++)
                {
                    b[i, j] = (byte)(255 - b[i, j]);
                }
            }
            return b;
        }

        //左右傾倒校正
        private void IncCorrect(byte[,] V)
        {
            int n = target_Collection.Count;//目標個數
            int wd = width_StandardFontImage + (width_StandardFontImage + 2) * n + width_StandardFontImage;//虛擬車牌二值化陣列寬度
            int mx = 0,mk = 0;//空白垂直線最大數，最佳偏移量
            byte[,] S0 = new byte[wd, height_StandardFontImage];//最佳校正之車牌二值化陣列
            for(int sx = -15; sx < 16; sx++)//嘗試偏移量
            {
                byte[,] S = new byte[wd, height_StandardFontImage];//虛擬車牌陣列
                double z = (double)sx / (height_StandardFontImage - 1);//每一畫素高度的X錯位量
                for(int j = 0; j < height_StandardFontImage; j++)
                {
                    int dx = (int)(z * (height_StandardFontImage - 1 - j));//X移動量
                    for(int i = 0; i < wd; i++)
                    {
                        int x = i + dx;
                        if (x < 0 || x > wd - 1) continue;
                        S[x, j] = V[i, j];
                    }
                }
                //計算錯位後的虛擬車牌中有幾個垂直空白線?越多表示字元越正直
                int n0 = 0;
                for(int i = 0; i < wd; i++)
                {
                    int m = 0;
                    for(int j = 0; j < height_StandardFontImage; j++)
                    {
                        m += S[i, j];
                    }
                    if (m == 0) n0 += 1;
                }
                if (n0 > mx)
                {
                    mx = n0;
                    mk = sx;
                    S0 = S;
                }
            }
            if (mk != 0)//需要調整傾倒字元，將修正後之車牌二值化影像重作目標擷取
            {
                arrayBinary = new byte[f.imagewidth, f.imageheight];
                for(int i = 0; i < wd; i++)
                {
                    for(int j = 0; j < height_StandardFontImage; j++)
                    {
                        arrayBinary[i + 10, j + 10] = S0[i, j];
                    }
                }
                arrayOutline = Outline(arrayBinary);//建立輪廓點陣列
                Atarget_Collection = getTargets(arrayOutline);//建立所有目標物件集合
                viewed_Target_Annotation = new bool[Atarget_Collection.Count];
                target_Collection = AlignTgs(Atarget_Collection);//群組化車牌目標
                n = target_Collection.Count;
                TgInfo[] T = new TgInfo[n];
                Array[] M = new Array[n];
                int[] w = new int[n];
                int[] h = new int[n];
                for(int k = 0; k < n; k++)
                {
                    TgInfo G = (TgInfo)target_Collection[k];
                    M[k] = Tg2Bin(G);//建立單一目標的二值化矩陣
                    T[k] = G;//儲存旋轉後的目標物件
                    w[k] = G.width;
                    h[k] = G.height;
                }
                Array.Sort(w);
                Array.Sort(h);
                int mw = w[n - 2];
                int mh = h[n - 2];
                for(int k = 0; k < n; k++)
                {
                    arrayBinary_NormalizationCompleted[k] = NmBin(T[k], (byte[,])M[k], mw, mh);
                }
            }
        }
    }
    
}
