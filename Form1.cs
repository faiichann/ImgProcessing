﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Sobel
{
    public partial class Form1 : Form
    {
        /* Defining Variables:
         * Image  >> to Store the Required Image.
         * Image2 >> output Image.
         * ImageData  >> to store The Image Data In the Memory (input image)
         * ImageData2 >> to store The Image Data In the Memory (output image)
         * buffer  >> buffering array used to edite the Image Data and to return back the edited ones to output array
         * buffer2 >> output array
         * grayscale >> to hold the grayscale value
         * r_x,g_x,b_x >> to hold the gradient in x components
         * r_y,g_y,b_y >> to hold the gradient in y components
         * r,g,b >> to hold the rgb values
         *  * brightness1 >> to hold the brightness value of current pixel
         * brightness2 >> to hold the brightness value of previous pixel
         * sub >> to hold the difference between brightness1 and brightness2
         * weights_x >> x-Kernel
         * weights_y >> y-Kernel
         * pointer  >> to hold the address to the red value of the first pixel in the memory (input array)
         * pointer2 >> to hold the address to the red value of the first pixel in the memory (output array)
         * location >> to hold the location of current pixel in input image
         * location >> to hold the location of current pixel in the window
         * weight_x >> to hold the x-weight 
         * weight_y >> to hold the y-weight
         */
        private Bitmap Image,Image2;
        private BitmapData ImageData,ImageData2;
        private byte[] buffer,buffer2;
        private int b,g,r,r_x,g_x,b_x, r_y, g_y, b_y,grayscale, location,location2, brightness1, brightness2, sub;
        private sbyte weight, weight_x,weight_y;
        private sbyte[,] weights;
        private sbyte[,] weights_x;
        private sbyte[,] weights_y;
        private IntPtr pointer,pointer2;
        public Form1()
        {
            InitializeComponent();
            weights_x = new sbyte[,] { { 1, 0, -1 }, 
                                       { 2, 0, -2 },
                                       { 1, 0, -1 } };
            weights_y = new sbyte[,] { { 1, 2, 1 }, 
                                       { 0, 0, 0 }, 
                                       { -1, -2, -1 } };
            weights = new sbyte[,] { { -1, -1, -1 },
                                     { -1, 9, -1 },
                                     { -1, -1, -1 } };
        }
        /* Saving the Image file:
         * type the file name followed by the file extension (for example new.jpg)
         */
        private void savebtn_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                pictureBox2.Image.Save(sfd.FileName,ImageFormat.Bmp);
            }
        }
        /* Loading the Image file
         * Showing it in the picturebox
         */
        private void loadbtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Image = new Bitmap(ofd.FileName);
                Image2 = new Bitmap(Image.Width, Image.Height);
            }
            pictureBox1.Image = Image;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Image = new Bitmap(pictureBox1.Image);
            Image2 = new Bitmap(Image.Width, Image.Height);
            for (int y = 0; y < Image.Height; y++)
            {
                for (int x = 0; x < Image.Width; x++)
                {
                    Color c = Image.GetPixel(x, y);
                    int red = c.R;
                    int green = c.G;
                    int blue = c.B;
                    float gray = (float)(red + green + blue) / 3.0f;
                    int igray = (int)gray;
                    Color cNew = Color.FromArgb(igray, igray, igray);
                    Image2.SetPixel(x, y, cNew);
                }
            }
            pictureBox2.Image = Image2;
        }

        /* Converting The Image file:
         * 1-Lock the Image Bits in the memory (PixelFormat.Format24bppRgb means that the program is going to lock only red , green and blue without including the alpha channel)
         * 2-initializing the buffer array it's going to have all the image data (the image have height and width which leads to total pixel count = height * width and each pixel have r,g,b so the array length = height*width*3)
         * 3-set the pointer to the location of the red value of the first pixel of the image
         * 4-copy the Image Data to the Buffer Array
         * 5-Loop through each pixel and make the loop step = 3 (i+=3)
         * 6-apply the window on the current pixel
         * 7-multiply each pixel in the window to each corresponding weight
         * 8-assign the channels total values to output array once the you finished looping through the window
         * 9-unlock the image bits
         */
        private void convertbtn_Click(object sender, EventArgs e)
        {
            ImageData  = Image.LockBits(new Rectangle (0,0,Image.Width,Image.Height),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
            ImageData2 = Image2.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            buffer  = new byte[ImageData.Stride * Image.Height];
            buffer2 = new byte[ImageData.Stride * Image.Height];
            pointer  = ImageData.Scan0;
            pointer2 = ImageData2.Scan0;
            Marshal.Copy(pointer, buffer, 0, buffer.Length);
            for (int y = 0; y < Image.Height ; y++)
            {
                for (int x = 0; x < Image.Width * 3; x+=3)
                {
                    r_x = g_x = b_x = 0; //reset the gradients in x-direcion values
                    r_y = g_y = b_y = 0; //reset the gradients in y-direction values
                    location = x + y * ImageData.Stride; //to get the location of any pixel >> location = x + y * Stride
                    for (int yy = -(int)Math.Floor(weights_y.GetLength(0) / 2.0d), yyy = 0; yy <= (int)Math.Floor(weights_y.GetLength(0) / 2.0d); yy++,yyy++)
                    {
                        if (y + yy >= 0 && y + yy < Image.Height) //to prevent crossing the bounds of the array
                        {
                            for (int xx = -(int)Math.Floor(weights_x.GetLength(1) / 2.0d) * 3, xxx = 0; xx <= (int)Math.Floor(weights_x.GetLength(1) / 2.0d) * 3; xx += 3, xxx++)
                            {
                                if (x + xx >= 0 && x + xx <= Image.Width * 3 - 3) //to prevent crossing the bounds of the array
                                {
                                    location2 = x + xx + (yy + y) * ImageData.Stride; //to get the location of any pixel >> location = x + y * Stride
                                    weight_x = weights_x[yyy, xxx];
                                    weight_y = weights_y[yyy, xxx];
                                    //applying the same weight to all channels
                                    b_x += buffer[location2] * weight_x;
                                    g_x += buffer[location2 + 1] * weight_x; //G_X
                                    r_x += buffer[location2 + 2] * weight_x;
                                    b_y += buffer[location2] * weight_y;
                                    g_y += buffer[location2 + 1] * weight_y;//G_Y
                                    r_y += buffer[location2 + 2] * weight_y;
                                }
                            }
                        }
                    }
                    //getting the magnitude for each channel
                    b = (int)Math.Sqrt(Math.Pow(b_x, 2) + Math.Pow(b_y, 2));
                    g = (int)Math.Sqrt(Math.Pow(g_x, 2) + Math.Pow(g_y, 2));//G
                    r = (int)Math.Sqrt(Math.Pow(r_x, 2) + Math.Pow(r_y, 2));

                    if (b > 255) b = 255;
                    if (g > 255) g = 255;
                    if (r > 255) r = 255;

                    //getting grayscale value
                    grayscale = (b + g + r) / 3;

                    //thresholding to clean up the background
                    //if (grayscale < 80) grayscale = 0;
                    buffer2[location] = (byte)grayscale;
                    buffer2[location + 1] = (byte)grayscale;
                    buffer2[location + 2] = (byte)grayscale;
                    //thresholding to clean up the background
                    //if (b < 100) b = 0;
                    //if (g < 100) g = 0;
                    //if (r < 100) r = 0;

                    //buffer2[location] = (byte)b;
                    //buffer2[location + 1] = (byte)g;
                    //buffer2[location + 2] = (byte)r;
                }
            }
            Marshal.Copy(buffer2, 0, pointer2, buffer.Length);
            Image.UnlockBits(ImageData);
            Image2.UnlockBits(ImageData2);
            pictureBox2.Image = Image2;
        }

        /* Converting The Image file:
         * 1-Lock the Image Bits in the memory (PixelFormat.Format24bppRgb means that the program is going to lock only red , green and blue without including the alpha channel)
         * 2-initializing the buffer array it's going to have all the image data (the image have height and width which leads to total pixel count = height * width and each pixel have r,g,b so the array length = height*width*3)
         * 3-set the pointer to the location of the blue value of the first pixel of the image
         * 4-copy the Image Data to the Buffer Array
         * 5-Loop through each pixel and make the loop step = 3 (i+=3)
         * 6-assigin each channel value to it's corresponding variable
         * 7-check for current pixel's channel values to find the biggest value that would be the brightness of the pixel (brightness1)
         * 8-check for previous pixel's channel values to find the biggest value that would be the brightness of the pixel (brightness2)
         * 9-subtract brightness1 and brightness2 and assigin the value to output array
         * 8-copy back the image Data2 from buffer to Image2 using the same pointer location
         * 9-unlock the image bits
         */
        private void button4_Click(object sender, EventArgs e)
        {
            ImageData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            ImageData2 = Image2.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            buffer = new byte[ImageData.Stride * Image.Height];
            buffer2 = new byte[ImageData.Stride * Image.Height];
            pointer = ImageData.Scan0;
            pointer2 = ImageData2.Scan0;
            Marshal.Copy(pointer, buffer, 0, buffer.Length);
            for (int i = 0; i < Image.Width * Image.Height * 3; i += 3)
            {
                if (i != 0)
                {
                    b = buffer[i];
                    g = buffer[i + 1];
                    r = buffer[i + 2];
                    if (b != g | b != r | g != r) brightness1 = Math.Max(Math.Max(r, g), b);
                    else brightness1 = b;
                    b = buffer[i - 3];
                    g = buffer[i - 2];
                    r = buffer[i - 1];
                    if (b != g | b != r | g != r) brightness2 = Math.Max(Math.Max(r, g), b);
                    else brightness2 = b;
                    sub = (byte)Math.Abs(brightness1 - brightness2);
                    buffer2[i] = (byte)sub;
                    buffer2[i + 1] = (byte)sub;
                    buffer2[i + 2] = (byte)sub;
                }
                else
                {
                    buffer2[i] = buffer[i];
                    buffer2[i + 1] = buffer[i + 1];
                    buffer2[i + 2] = buffer[i + 2];
                }
            }
            Marshal.Copy(buffer2, 0, pointer2, buffer.Length);
            Image.UnlockBits(ImageData);
            Image2.UnlockBits(ImageData2);
            pictureBox2.Image = Image2;
        }
          /* Converting The Image file:
         * 1-Lock the Image Bits in the memory (PixelFormat.Format24bppRgb means that the program is going to lock only red , green and blue without including the alpha channel)
         * 2-initializing the buffer array it's going to have all the image data (the image have height and width which leads to total pixel count = height * width and each pixel have r,g,b so the array length = height*width*3)
         * 3-set the pointer to the location of the blue value of the first pixel of the image
         * 4-copy the Image Data to the Buffer Array
         * 5-Loop through each pixel and make the loop step = 3 (i+=3)
         * 6-assigin each channel value to it's corresponding variable
         * 7-subtract 255 from each channel
         * 8-copy back the image Data from buffer to Image using the same pointer location
         * 9-unlock the image bits
         */
        private void button2_Click(object sender, EventArgs e)
        {
            ImageData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            buffer = new byte[3 * Image.Width * Image.Height];
            pointer = ImageData.Scan0;
            Marshal.Copy(pointer, buffer, 0, buffer.Length);
            for (int i = 0; i < Image.Height * 3 * Image.Width; i += 3)
            {
                b = buffer[i];
                g = buffer[i + 1];
                r = buffer[i + 2];
                buffer[i] = (byte)(255 - r);
                buffer[i + 1] = (byte)(255 - g);
                buffer[i + 2] = (byte)(255 - b);
            }
            Marshal.Copy(buffer, 0, pointer, buffer.Length);
            Image.UnlockBits(ImageData);
            pictureBox2.Image = Image;
        }
                /* Converting The Image file:
         * 1-Lock the Image Bits in the memory (PixelFormat.Format24bppRgb means that the program is going to lock only red , green and blue without including the alpha channel)
         * 2-initializing the buffer array it's going to have all the image data (the image have height and width which leads to total pixel count = height * width and each pixel have r,g,b so the array length = height*width*3)
         * 3-set the pointer to the location of the red value of the first pixel of the image
         * 4-copy the Image Data to the Buffer Array
         * 5-Loop through each pixel and make the loop step = 3 (i+=3)
         * 6-apply the window on the current pixel
         * 7-multiply each pixel in the window to each corresponding weight then add the value to it's corresponding channel 
         * 8-assign the channels total values to output array once the you finished looping through the window
         * 9-unlock the image bits
         */
        private void button3_Click(object sender, EventArgs e)
        {
            ImageData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            ImageData2 = Image2.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            buffer = new byte[ImageData.Stride * Image.Height];
            buffer2 = new byte[ImageData.Stride * Image.Height];
            pointer = ImageData.Scan0;
            pointer2 = ImageData2.Scan0;
            Marshal.Copy(pointer, buffer, 0, buffer.Length);
            for (int y = 0; y < Image.Height; y++)
            {
                for (int x = 0; x < Image.Width * 3; x += 3)
                {
                    r = g = b = 0; //reset the channels values
                    location = x + y * ImageData.Stride; //to get the location of any pixel >> location = x + y * Stride
                    for (int yy = -(int)Math.Floor(weights.GetLength(0) / 2.0d), yyy = 0; yy <= (int)Math.Floor(weights.GetLength(0) / 2.0d); yy++, yyy++)
                    {
                        if (y + yy >= 0 && y + yy < Image.Height) //to prevent crossing the bounds of the array
                        {
                            for (int xx = -(int)Math.Floor(weights.GetLength(1) / 2.0d) * 3, xxx = 0; xx <= (int)Math.Floor(weights.GetLength(1) / 2.0d) * 3; xx += 3, xxx++)
                            {
                                if (x + xx >= 0 && x + xx <= Image.Width * 3 - 3) //to prevent crossing the bounds of the array
                                {
                                    location2 = x + xx + (yy + y) * ImageData.Stride; //to get the location of any pixel >> location = x + y * Stride
                                    weight = weights[yyy, xxx];
                                    //applying the same weight to all channels
                                    b += buffer[location2] * weight;
                                    g += buffer[location2 + 1] * weight;
                                    r += buffer[location2 + 2] * weight;
                                }
                            }
                        }
                    }
                    if (b > 255) b = 255;
                    else if (b < 0) b = 0;
                    if (g > 255) g = 255;
                    else if (g < 0) g = 0;
                    if (r > 255) r = 255;
                    else if (r < 0) r = 0;
                    buffer2[location] = (byte)b;
                    buffer2[location + 1] = (byte)g;
                    buffer2[location + 2] = (byte)r;
                }
            }
            Marshal.Copy(buffer2, 0, pointer2, buffer.Length);
            Image.UnlockBits(ImageData);
            Image2.UnlockBits(ImageData2);
            pictureBox2.Image = Image2;
        }
    }
}