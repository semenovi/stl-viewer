using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace STLViewer
{
  public class Vector3
  {
    public float X, Y, Z;

    public Vector3(float x, float y, float z)
    {
      X = x;
      Y = y;
      Z = z;
    }

    public static Vector3 operator -(Vector3 a, Vector3 b)
    {
      return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public void Normalize()
    {
      float length = (float)Math.Sqrt(X * X + Y * Y + Z * Z);
      if (length > 0)
      {
        X /= length;
        Y /= length;
        Z /= length;
      }
    }

    public static float DotProduct(Vector3 a, Vector3 b)
    {
      return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    public static Vector3 CrossProduct(Vector3 a, Vector3 b)
    {
      return new Vector3(
          a.Y * b.Z - a.Z * b.Y,
          a.Z * b.X - a.X * b.Z,
          a.X * b.Y - a.Y * b.X
      );
    }
  }

  public class Triangle
  {
    public Vector3 Normal;
    public Vector3 Vertex1;
    public Vector3 Vertex2;
    public Vector3 Vertex3;
    public Color Color;

    public Triangle(Vector3 normal, Vector3 v1, Vector3 v2, Vector3 v3)
    {
      Normal = normal;
      Vertex1 = v1;
      Vertex2 = v2;
      Vertex3 = v3;
      Color = Color.Gray;
    }

    public void CalculateNormal()
    {
      // Вычисляем векторы граней
      Vector3 edge1 = Vertex2 - Vertex1;
      Vector3 edge2 = Vertex3 - Vertex1;
      // Нормаль - векторное произведение
      Normal = Vector3.CrossProduct(edge1, edge2);
      Normal.Normalize();
    }
  }

  public class MainForm : Form
  {
    private List<Triangle> triangles = new List<Triangle>();
    private float rotationX = 0;
    private float rotationY = 0;
    private float scale = 100;
    private Point lastMousePos;
    private RenderMode currentMode = RenderMode.Wireframe;
    private Vector3 lightDirection = new Vector3(-0.5f, -1f, -0.5f); // Направление света (сверху под углом)
    private Vector3 cameraDirection = new Vector3(0, 0, 1); // Направление камеры (смотрим вдоль оси Z)

    private enum RenderMode
    {
      Wireframe,
      Material,
      Lighting
    }

    public MainForm()
    {
      this.Size = new Size(800, 600);
      this.DoubleBuffered = true;

      // Обработчики мыши
      this.MouseDown += (s, e) => { lastMousePos = e.Location; };
      this.MouseMove += (s, e) =>
      {
        if (e.Button == MouseButtons.Left)
        {
          rotationY -= (e.X - lastMousePos.X) * 0.01f;
          rotationX += (e.Y - lastMousePos.Y) * 0.01f;
          lastMousePos = e.Location;
          this.Invalidate();
        }
      };
      this.MouseWheel += (s, e) =>
      {
        scale += e.Delta * 0.1f;
        scale = Math.Max(10, scale);
        this.Invalidate();
      };

      // Обработчик клавиш
      this.KeyDown += (s, e) =>
      {
        switch (e.KeyCode)
        {
          case Keys.D1:
            currentMode = RenderMode.Wireframe;
            break;
          case Keys.D2:
            currentMode = RenderMode.Material;
            break;
          case Keys.D3:
            currentMode = RenderMode.Lighting;
            break;
        }
        this.Invalidate();
      };

      LoadSTL("model.stl");
      lightDirection.Normalize();
    }

    private void LoadSTL(string filename)
    {
      try
      {
        using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
        {
          reader.ReadBytes(80); // Пропускаем заголовок
          uint triangleCount = reader.ReadUInt32();

          for (int i = 0; i < triangleCount; i++)
          {
            float nx = reader.ReadSingle();
            float ny = reader.ReadSingle();
            float nz = reader.ReadSingle();

            float x1 = reader.ReadSingle();
            float y1 = reader.ReadSingle();
            float z1 = reader.ReadSingle();

            float x2 = reader.ReadSingle();
            float y2 = reader.ReadSingle();
            float z2 = reader.ReadSingle();

            float x3 = reader.ReadSingle();
            float y3 = reader.ReadSingle();
            float z3 = reader.ReadSingle();

            reader.ReadUInt16(); // Пропускаем атрибут

            var triangle = new Triangle(
                new Vector3(nx, ny, nz),
                new Vector3(x1, y1, z1),
                new Vector3(x2, y2, z2),
                new Vector3(x3, y3, z3)
            );
            triangle.CalculateNormal(); // Пересчитываем нормаль
            triangles.Add(triangle);
          }
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при загрузке STL файла: {ex.Message}");
      }
    }

    private Point Project(Vector3 point)
    {
      // Вращение точки
      float x = point.X;
      float y = point.Y * (float)Math.Cos(rotationX) - point.Z * (float)Math.Sin(rotationX);
      float z = point.Y * (float)Math.Sin(rotationX) + point.Z * (float)Math.Cos(rotationX);

      float x2 = x * (float)Math.Cos(rotationY) + z * (float)Math.Sin(rotationY);
      float z2 = -x * (float)Math.Sin(rotationY) + z * (float)Math.Cos(rotationY);

      // Проекция на экран
      int screenX = (int)(x2 * scale + this.Width / 2);
      int screenY = (int)(y * scale + this.Height / 2);

      return new Point(screenX, screenY);
    }

    private Vector3 RotateVector(Vector3 v)
    {
      // Применяем те же преобразования, что и к точкам, но без смещения
      float x = v.X;
      float y = v.Y * (float)Math.Cos(rotationX) - v.Z * (float)Math.Sin(rotationX);
      float z = v.Y * (float)Math.Sin(rotationX) + v.Z * (float)Math.Cos(rotationX);

      float x2 = x * (float)Math.Cos(rotationY) + z * (float)Math.Sin(rotationY);
      float z2 = -x * (float)Math.Sin(rotationY) + z * (float)Math.Cos(rotationY);

      return new Vector3(x2, y, z2);
    }

    private bool IsTriangleFacingCamera(Triangle triangle)
    {
      // Поворачиваем нормаль в соответствии с поворотом модели
      Vector3 rotatedNormal = RotateVector(triangle.Normal);
      // Вычисляем скалярное произведение между нормалью и направлением камеры
      float dotProduct = Vector3.DotProduct(rotatedNormal, cameraDirection);
      // Грань видима, если скалярное произведение отрицательное
      return dotProduct < 0;
    }

    private float CalculateLighting(Vector3 normal)
    {
      Vector3 rotatedNormal = RotateVector(normal);
      rotatedNormal.Normalize();

      // Инвертируем направление света, так как теперь нормали направлены наружу
      float intensity = Vector3.DotProduct(rotatedNormal, lightDirection);
      intensity = Math.Max(0.2f, Math.Min(1.0f, intensity)); // Ambient light = 0.2
      return intensity;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);
      e.Graphics.Clear(Color.Black);
      e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

      // Сортируем треугольники по глубине для корректного отображения
      var sortedTriangles = triangles.OrderBy(t =>
      {
        var center = new Vector3(
            (t.Vertex1.X + t.Vertex2.X + t.Vertex3.X) / 3,
            (t.Vertex1.Y + t.Vertex2.Y + t.Vertex3.Y) / 3,
            (t.Vertex1.Z + t.Vertex2.Z + t.Vertex3.Z) / 3
        );
        var rotated = RotateVector(center);
        return -rotated.Z; // Сортировка от дальних к ближним
      }).ToList();

      foreach (var triangle in sortedTriangles)
      {
        // Проверяем видимость грани
        if (!IsTriangleFacingCamera(triangle))
          continue;

        Point p1 = Project(triangle.Vertex1);
        Point p2 = Project(triangle.Vertex2);
        Point p3 = Project(triangle.Vertex3);
        Point[] points = { p1, p2, p3 };

        switch (currentMode)
        {
          case RenderMode.Wireframe:
            using (Pen pen = new Pen(Color.White))
            {
              e.Graphics.DrawPolygon(pen, points);
            }
            break;

          case RenderMode.Material:
            using (SolidBrush brush = new SolidBrush(triangle.Color))
            {
              e.Graphics.FillPolygon(brush, points);
            }
            using (Pen pen = new Pen(Color.DarkGray))
            {
              e.Graphics.DrawPolygon(pen, points);
            }
            break;

          case RenderMode.Lighting:
            float intensity = CalculateLighting(triangle.Normal);
            int colorValue = (int)(intensity * 255);
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(colorValue, colorValue, colorValue)))
            {
              e.Graphics.FillPolygon(brush, points);
            }
            break;
        }
      }
    }

  }

  static class Program
  {
    [STAThread]
    static void Main()
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new MainForm());
    }
  }
}