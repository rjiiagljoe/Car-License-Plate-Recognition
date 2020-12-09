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
        int minHeight = 16;
        int maxHeight = 100;
        int minWidth = 2;
        int maxWidth = 100;
        byte[,] arrayGray;//灰階陣列
        byte[,] arrayBinary;//二值化陣列
        byte[,] arrayOutline;//輪廓線陣列
        ArrayList target_Collection;//目標物件集合
        Bitmap basemap_Copy;//底圖副本

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
            arrayOutline = Outline(arrayBinary);
            pictureBox1.Image = f.BWImg(arrayOutline);
        }
        //選擇顯示某目標之輪廓線
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (arrayOutline == null) return;
                int x = e.X;
                int y = e.Y;
                //尋找左方最近之輪廓點
                while(arrayOutline[x,y]==0 && x>0)
                {
                    x--;
                }
                ArrayList A = getGrp(arrayOutline, x, y);//搜尋此目標所有輪廓點
                Bitmap bmp = f.BWImg(arrayOutline);//建立輪廓圖
                for(int k=0;k<A.Count;k++)
                {
                    Point p = (Point)A[k];
                    bmp.SetPixel(p.X, p.Y, Color.Red);
                }
                pictureBox1.Image = bmp;
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
                                    G.targetPointList.Add(k);
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
                    A.Add(G);//加入有效目標集合
                }
            }
            return A;//回傳目標物件集合
        }

        //建立目標物件
        private void targetsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            target_Collection = getTargets(arrayOutline);//建立目標物件集合
            //繪製目標輪廓點
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
    }
}
