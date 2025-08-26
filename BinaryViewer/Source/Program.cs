using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Avalonia.Markup.Xaml;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;


namespace BinaryViewer;


class Program
{
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
                    .UsePlatformDetect()
                    .LogToTrace()
                    .UseReactiveUI();
}

public class App : Application
{
    private const int _width = 1000;
    private const int _height = 1000;
    private const int BarsCount = 241;

    private readonly DataType[] allTypes = Enum.GetValues<DataType>();

    private byte[] _openedFileBytes = [];
    private List<StackPanel> _dataColumns = [];
    public const int MaxFileSize = int.MaxValue;

    // private ObservableCollection<Bar> DataGridItems = [];
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        TextBlock fileInfo = new()
        {
            Name = "FileInfo",
            Text = "File Info",
            Margin = new Thickness(0, 10, 0, 0),
            FontSize = 20,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        DataGrid dataGrid = new()
        {
            Name = "DataGrid",
            AutoGenerateColumns = true,
            IsReadOnly = true,
            SelectionMode = DataGridSelectionMode.Single,
            Margin = new Thickness(0, 10, 0, 0),
            // Height = ,
            Width = 200,
        };

        ComboBox dataColumnTypeSelector = new()
        {
            Name = "DataColumnTypeSelector",
            Width = 150,
            ItemsSource = allTypes,
            SelectedIndex = 0
        };

        TextBlock dataColumnOffsetText = new()
        {
            Name = "DataColumnOffsetText",
            Text = "Offset: ",
            IsVisible = true
        };

        TextBox dataColumnOffsetInput = new()
        {
            Name = "DataColumnOffsetInput",
            Width = 50,
            Watermark = "Offset",
            IsVisible = true,
            Text = "0"
        };

        TextBlock dataColumnPaddingText = new()
        {
            Name = "DataColumnPaddingText",
            Text = "Padding: ",
            IsVisible = true
        };

        TextBox dataColumnPaddingInput = new()
        {
            Name = "DataColumnPaddingInput",
            Width = 50,
            Watermark = "Padding",
            IsVisible = true,
            Text = "0",

        };

        TextBlock dataColumnStrLenText = new()
        {
            Name = "DataColumnStrLenText",
            Text = "String Length: ",
            IsVisible = true
        };

        TextBox dataColumnStrLenInput = new()
        {
            Name = "DataColumnStrLenInput",
            Width = 50,
            Watermark = "String Length",
            IsVisible = true,
            Text = "0"
        };

        Button dataColumnOkButton = new()
        {
            Name = "DataColumnOkButton",
            Content = "OK",
            Width = 40,
            IsVisible = true
        };
        dataColumnOkButton.Click += (sender, args) =>
        {
            if (_openedFileBytes.Length == 0) return;

            int offset = int.Parse(dataColumnOffsetInput.Text);
            int padding = int.Parse(dataColumnPaddingInput.Text);
            DataType? selectedType = (DataType?)dataColumnTypeSelector.SelectedItem;
            int strLen = int.Parse(dataColumnStrLenInput.Text);

            List<string> columnItems = GetStrings(selectedType, offset, padding, BarsCount, strLen);
            dataGrid.ItemsSource = columnItems;
        };

        StackPanel dataColumnProperty = new()
        {
            Name = "DataColumnProperty",
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Margin = new Thickness(0, 10, 0, 0),
            IsVisible = true,
            Children = {
                dataColumnTypeSelector,
                dataColumnOffsetText,
                dataColumnOffsetInput,
                dataColumnPaddingText,
                dataColumnPaddingInput,
                dataColumnStrLenText,
                dataColumnStrLenInput,
                dataColumnOkButton
            }
        };

        StackPanel dataColumn = new()
        {
            Name = "DataColumn",
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Margin = new Thickness(0, 10, 0, 0),
            Children =
            {
                dataColumnProperty,
                dataGrid
            }
        };

        TextBlock binaryDataBlock = new()
        {
            Width = 200,
            Name = "BinaryDataView",
            Text = "No data",
            IsVisible = false
        };

        var filePathInput = new TextBox
        {
            Width = 600,
            Name = "FilePathInput",
            Watermark = "Enter file path"
        };

        var openButton = new Button
        {
            Content = "Open",
            Width = 60
        };

        var searchInput = new TextBox
        {
            Width = 100,
            Name = "SearchInput",
            Watermark = "Search",
            IsVisible = true
        };

        var searchType = new ComboBox
        {
            Width = 100,
            Name = "SearchType",
            ItemsSource = allTypes,
            SelectedIndex = 0,
            IsVisible = true
        };

        var revertBytesCheckBox = new CheckBox
        {
            Name = "RevertBytes",
            Content = "Revert Bytes",
            IsVisible = true,
            IsChecked = false
        };

        var searchSequenceCheckBox = new CheckBox
        {
            Name = "SearchSequence",
            Content = "Sequence",
            IsVisible = true,
            IsChecked = false
        };

        var searchResult = new TextBox
        {
            Name = "SearchResult",
            Text = "Search Result",
            IsReadOnly = true,
            IsVisible = true,
            Background = Avalonia.Media.Brushes.Transparent,
            Margin = new Thickness(0, 10, 0, 0),

        };

        var searchButton = new Button
        {
            Content = "Search",
            Width = 60,
            IsVisible = true
        };
        searchButton.Click += (sender, args) =>
        {
            if (_openedFileBytes.Length == 0) return;
            if (searchInput.Text == null) return;

            byte[] bytesToSearch;
            List<string> results = [];
            string queryText = searchInput.Text;

            if (searchSequenceCheckBox.IsChecked.GetValueOrDefault())
            {
                string[] queries = searchInput.Text.Split(',');
                List<byte[]> bytesToSearchList = [];
                foreach (string querry in queries)
                {
                    byte[] queryBytes = GetBytesToSeach((DataType?)searchType.SelectedItem, querry.Trim(), revertBytesCheckBox.IsChecked.Value);
                    bytesToSearchList.Add(queryBytes);
                }
                bytesToSearch = [.. bytesToSearchList.SelectMany(b => b)];
            }
            else
            {
                string query = searchInput.Text;
                bytesToSearch = GetBytesToSeach((DataType?)searchType.SelectedItem, query, revertBytesCheckBox.IsChecked.Value);
                if (bytesToSearch.Length == 0) return;

            }
            // Perform search
            for (int i = 0; i <= _openedFileBytes.Length - bytesToSearch.Length; i++)
            {
                byte[] subArray = _openedFileBytes[i..(i + bytesToSearch.Length)];
                if (bytesToSearch.SequenceEqual(subArray))
                {
                    // both decimal and hex offset
                    string idxStr = $"{i}/0x{i:X}";
                    results.Add(idxStr);
                    // Optionally, you can also display the found data in the binaryDataBlock
                }
            }
            if (results.Count == 0)
            {
                searchResult.Text = $"not found";
            }
            else
            {
                searchResult.Text = string.Join(", ", results);
            }
        };

        var searchRow = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Children =
            {
                searchInput,
                searchType,
                revertBytesCheckBox,
                searchSequenceCheckBox,
                searchButton,
                searchResult
            }
        };

        // load the file into RAM
        openButton.Click += (sender, args) =>
        {
            string? filePath = filePathInput.Text;
            if (filePath == null)
            {
                fileInfo.Text = "Please enter a valid file path.";
                return;
            }

            if (!File.Exists(filePath))
            {
                fileInfo.Text = "File does not exist.";
                return;
            }

            FileInfo fileinfo = new(filePath);
            if (fileinfo.Length > MaxFileSize)
            {
                fileInfo.Text = "File is too large to open.";
                return;
            }

            _openedFileBytes = File.ReadAllBytes(filePath);
            fileInfo.Text = $"({_openedFileBytes.Length} bytes){filePath}";

        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Window
            {
                Title = "Binary Data Viewer",
                Width = 1000,
                Height = 1000,
                Content = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Vertical,
                    Children =
                    {
                        // file path and open button
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            // HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                            Children =
                            {
                                filePathInput,
                                openButton
                            }
                        },

                        fileInfo,
                        searchRow,
                        dataColumn,
                        binaryDataBlock
                    }
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void NewDataRow()
    {
        TextBlock textBlock = new() { Text = "Select data type" };
        ComboBox typeSelector = new()
        {
            Width = 150,
            Name = "Type",
            ItemsSource = allTypes,
            SelectedIndex = 0
        };

        var offsetInput = new TextBox
        {
            Width = 50,
            Name = "OffsetInput",
            Watermark = "Offset",
            IsVisible = false,
            Text = "0"
        };

        var strLenInput = new TextBox
        {
            Width = 50,
            Name = "StrLenInput",
            Watermark = "Length",
            IsVisible = false,
            Text = "0"
        };

        typeSelector.SelectionChanged += (sender, args) =>
        {
            textBlock.Text = BinaryToString(
                (DataType?)typeSelector.SelectedItem,
                _openedFileBytes,
                int.Parse(offsetInput.Text),
                int.Parse(strLenInput.Text)
            );
        };

        var dataRow = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Children =
            {
                textBlock,
            }
        };

        _dataColumns.Add(dataRow);
    }

    private List<string> GetStrings(DataType? type, int offset, int padding, int count, int strLen = 0)
    {
        List<string> results = [];

        switch (type)
        {
            case DataType.Int32:
                if (offset + (sizeof(int) + padding) * count > _openedFileBytes.Length)
                    break;
                for (int i = 0; i < count; i++)
                {
                    int currentOffset = offset + i * (sizeof(int) + padding);

                    int value = BitConverter.ToInt32(_openedFileBytes, currentOffset);
                    results.Add(value.ToString());
                }
                break;
            // Add other cases for Int64, Uint32, Uint64, Float, Double
            case DataType.Int64:
                if (offset + (sizeof(long) + padding) * count > _openedFileBytes.Length)
                    break;
                for (int i = 0; i < count; i++)
                {
                    int currentOffset = offset + i * (sizeof(long) + padding);

                    long value = BitConverter.ToInt64(_openedFileBytes, currentOffset);
                    results.Add(value.ToString());
                }
                break;
            case DataType.Double:
                if (offset + (sizeof(double) + padding) * count > _openedFileBytes.Length)
                    break;
                for (int i = 0; i < count; i++)
                {
                    int currentOffset = offset + i * (sizeof(double) + padding);

                    double value = BitConverter.ToDouble(_openedFileBytes, currentOffset);
                    results.Add(value.ToString());
                }
                break;
            case DataType.String:
                if (offset + (padding + strLen) * count > _openedFileBytes.Length)
                    break;
                for (int i = 0; i < count; i++)
                {
                    int currentOffset = offset + i * (padding + strLen);

                    string value = System.Text.Encoding.UTF8.GetString(_openedFileBytes, currentOffset, strLen);
                    results.Add(value);
                }
                break;
            default:
                results.Add("Unsupported data type");
                break;
        }
        return results;
    }

    public static string BinaryToString(DataType? type, byte[] binaryData, int offset, int stringLen = 0)
    {
        return type switch
        {
            DataType.String => System.Text.Encoding.UTF8.GetString(binaryData, offset, stringLen),
            DataType.Int32 => BitConverter.ToInt32(binaryData, offset).ToString(),
            DataType.Int64 => BitConverter.ToInt64(binaryData, offset).ToString(),
            DataType.Uint32 => BitConverter.ToUInt32(binaryData, offset).ToString(),
            DataType.Uint64 => BitConverter.ToUInt64(binaryData, offset).ToString(),
            DataType.Float => BitConverter.ToSingle(binaryData, offset).ToString(),
            DataType.Double => BitConverter.ToDouble(binaryData, offset).ToString(),
            _ => "Select data type",
        };
    }

    public static byte[] GetBytesToSeach(DataType? type, string searchText, bool revert = false)
    {
        byte[] bytes;
        switch (type)
        {
            case DataType.String:
                bytes = System.Text.Encoding.UTF8.GetBytes(searchText.Trim());
                break;
            case DataType.Int32:
                bytes = BitConverter.GetBytes(int.Parse(searchText));
                break;
            case DataType.Int64:
                bytes = BitConverter.GetBytes(long.Parse(searchText));
                break;
            case DataType.Uint32:
                bytes = BitConverter.GetBytes(uint.Parse(searchText));
                break;
            case DataType.Uint64:
                bytes = BitConverter.GetBytes(ulong.Parse(searchText));
                break;
            case DataType.Float:
                bytes = BitConverter.GetBytes(float.Parse(searchText));
                break;
            case DataType.Double:
                bytes = BitConverter.GetBytes(double.Parse(searchText));
                break;
            case DataType.Hex:
                bytes = HexStringToByteArray(searchText);
                break;
            default:
                return [];
                // break;
        };
        return revert ? [.. bytes.Reverse()] : bytes;
    }

    public static byte[] HexStringToByteArray(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return [];

        // Remove any whitespace
        hex = hex.Replace(" ", "").Replace("-", "");

        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have an even length.");

        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return bytes;
    }
}