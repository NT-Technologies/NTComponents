using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTFileUpload_Tests : BunitContext {

    private sealed class TestModel {
        public IReadOnlyList<IBrowserFile>? Files { get; set; }
    }

    [Fact]
    public void Static_Renderer_Renders_Native_File_Input_For_Form_Post() {
        SetRendererInfo(new RendererInfo("Static", false));
        var model = new TestModel();

        var cut = RenderFileUpload(model, parameters => parameters
            .Add(p => p.ElementId, "resume")
            .Add(p => p.Label, "Resume")
            .Add(p => p.SupportingText, "Attach a PDF")
            .Add(p => p.Accept, ".pdf")
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> {
                ["name"] = "resumeFile",
                ["required"] = true
            }));

        var input = cut.Find("input[type=file]");
        input.GetAttribute("id").Should().Be("resume");
        input.GetAttribute("name").Should().Be("resumeFile");
        input.GetAttribute("accept").Should().Be(".pdf");
        input.HasAttribute("required").Should().BeTrue();
        cut.Find(".nt-input-container").TagName.Should().Be("DIV");
        input.GetAttribute("aria-labelledby").Should().Be("resume-label");
        input.GetAttribute("aria-describedby").Should().Contain("resume-supporting");
        cut.Find("#resume-supporting").TextContent.Should().Be("Attach a PDF");
        // The library module shows the chosen file names for marked static inputs, since no .NET handler runs.
        input.GetAttribute("data-nt-file-upload-static").Should().Be("true");
    }

    [Fact]
    public void Interactive_Renderer_Does_Not_Mark_File_Input_As_Static() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = RenderFileUpload(new TestModel(), parameters => parameters.Add(p => p.Label, "Resume"));

        cut.Find("input[type=file]").HasAttribute("data-nt-file-upload-static").Should().BeFalse();
    }

    [Fact]
    public void Interactive_Renderer_Renders_Upload_Action_Only_When_Upload_Handler_Exists() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        using var withoutHandler = RenderFileUpload(configure: parameters => parameters.Add(p => p.ShowUploadButton, true));
        withoutHandler.FindAll(".nt-file-upload-action").Should().BeEmpty();

        using var withHandler = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.OnUploadFile, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, _ => Task.CompletedTask))
            .Add(p => p.ShowUploadButton, true));

        var action = withHandler.Find(".nt-file-upload-action");
        action.TextContent.Should().Be("Upload");
        action.HasAttribute("disabled").Should().BeTrue();
        action.ParentElement!.ClassList.Should().Contain("nt-input-field");
        action.ParentElement!.TagName.Should().NotBe("LABEL");
    }

    [Fact]
    public void Prefix_And_Suffix_Are_Rendered_And_Described_By_File_Input() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.PrefixText, "File")
            .Add(p => p.SuffixText, "PDF"));

        var describedBy = cut.Find("input[type=file]").GetAttribute("aria-describedby")!;
        describedBy.Should().Contain(cut.Find(".nt-input-prefix").GetAttribute("id"));
        describedBy.Should().Contain(cut.Find(".nt-input-suffix").GetAttribute("id"));
        cut.Find(".nt-input-prefix").TextContent.Should().Be("File");
        cut.Find(".nt-input-suffix").TextContent.Should().Be("PDF");
    }

    [Fact]
    public void Error_State_Renders_Error_Adornment_Instead_Of_Trailing_Icon() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.MaximumFileSize, 4)
            .Add(p => p.TrailingIcon, MaterialIcon.Info)
            .Add(p => p.OnUploadFile, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, _ => Task.CompletedTask)));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("large", "large.txt"));

        cut.Find(".nt-input-error-icon").TextContent.Should().Contain(MaterialIcon.Error.Icon);
        cut.FindAll(".nt-input-trailing").Should().BeEmpty();
    }

    [Fact]
    public void Multiple_And_Accept_Are_Applied_To_Interactive_File_Input() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.Multiple, true)
            .Add(p => p.Accept, "image/*"));

        var input = cut.Find("input[type=file]");
        input.HasAttribute("multiple").Should().BeTrue();
        input.GetAttribute("accept").Should().Be("image/*");
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData("multiple", true)]
    [InlineData("", true)]
    [InlineData("false", false)]
    [InlineData("FALSE", false)]
    public void Additional_Multiple_Attribute_Uses_Html_Boolean_Semantics(object value, bool expected) {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> { ["multiple"] = value }));

        cut.Find("input[type=file]").HasAttribute("multiple").Should().Be(expected);
    }

    [Fact]
    public async Task ClearAsync_Clears_Bound_Value_And_Selection_Text() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var model = new TestModel {
            Files = [new FakeBrowserFile("a.txt", 12)]
        };

        var cut = RenderFileUpload(model);

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-input-has-value");
        cut.Find(".nt-file-upload-value").TextContent.Should().Be("a.txt");

        await cut.Instance.ClearAsync();
        cut.Render();

        model.Files.Should().BeNull();
        cut.Find(".nt-file-upload-value").TextContent.Should().Be("No file selected");
        cut.FindAll(".nt-file-upload-item").Should().BeEmpty();
    }

    [Fact]
    public void ReadOnly_Disables_File_Selection_Instead_Of_Rendering_Readonly() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = RenderFileUpload(configure: parameters => parameters.Add(p => p.ReadOnly, true));

        var input = cut.Find("input[type=file]");
        input.HasAttribute("disabled").Should().BeTrue();
        input.HasAttribute("readonly").Should().BeFalse();
    }

    [Fact]
    public void Oversized_File_Selection_Shows_Immediate_Error_State() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var model = new TestModel();
        NTFileUploadEventArgs? errorArgs = null;

        var cut = RenderFileUpload(model, parameters => parameters
            .Add(p => p.MaximumFileSize, 4)
            .Add(p => p.OnUploadFile, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, _ => Task.CompletedTask))
            .Add(p => p.OnFileError, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, args => errorArgs = args)));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("large", "large.txt"));

        model.Files.Should().BeNull();
        cut.Find(".nt-input").ClassList.Should().NotContain("nt-invalid");
        cut.Find(".nt-input").ClassList.Should().NotContain("nt-modified");
        cut.Find("input[type=file]").GetAttribute("aria-invalid").Should().Be("true");
        cut.Find(".nt-input-error-text").TextContent.Should().Be("large.txt is too large. Maximum file size is 4 bytes.");
        cut.Find(".nt-file-upload-value").TextContent.Should().Be("large.txt");
        cut.Find(".nt-file-upload-status").TextContent.Should().Be("Too large");
        cut.Find(".nt-file-upload-action").HasAttribute("disabled").Should().BeTrue();

        var progress = cut.Find(".nt-file-upload-progress .nt-progress");
        progress.GetAttribute("style")!.Should().Contain("--nt-progress-indicator-color:var(--nt-color-error)");
        progress.GetAttribute("style")!.Should().Contain("--nt-progress-track-color:var(--nt-color-error-container)");
        progress.GetAttribute("aria-valuenow").Should().Be("100");

        errorArgs.Should().NotBeNull();
        errorArgs!.Name.Should().Be("large.txt");
        errorArgs.ErrorMessage.Should().Be("large.txt is too large. Maximum file size is 4 bytes.");
    }

    [Fact]
    public void Selected_File_Displays_Status_Percentage() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.OnUploadFile, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, _ => Task.CompletedTask)));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("small", "small.txt"));

        cut.Find(".nt-file-upload-status span:first-child").TextContent.Should().Be("Ready");
        cut.Find(".nt-file-upload-percent").TextContent.Should().Be("0%");
        cut.Find(".nt-file-upload-list").HasAttribute("aria-live").Should().BeFalse();
    }

    [Fact]
    public void Completed_Upload_Hides_Status_Percentage() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.OnUploadFile, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, _ => Task.CompletedTask))
            .Add(p => p.ShowUploadButton, true));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("small", "small.txt"));
        cut.Find(".nt-file-upload-action").Click();

        cut.WaitForAssertion(() => {
            cut.Find(".nt-file-upload-status").TextContent.Should().Be("Complete");
            cut.FindAll(".nt-file-upload-percent").Should().BeEmpty();
        });
    }

    [Fact]
    public void Zero_Maximum_File_Count_Rejects_Selection_And_Reports_The_Configuration_Error() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var model = new TestModel();
        NTFileUploadEventArgs? error = null;
        var cut = RenderFileUpload(model, parameters => parameters
            .Add(p => p.MaximumFileCount, 0)
            .Add(p => p.OnFileError, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, args => error = args)));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("payload", "file.txt"));

        model.Files.Should().BeNull();
        error.Should().NotBeNull();
        error!.ErrorMessage.Should().Be("MaximumFileCount must be greater than zero.");
        cut.Find(".nt-input-error-text").TextContent.Should().Be(error.ErrorMessage);
        cut.FindAll(".nt-file-upload-item").Should().BeEmpty();
    }

    [Theory]
    [InlineData(1, 2, "Select 1 file or fewer.")]
    [InlineData(2, 3, "Select 2 files or fewer.")]
    public void Too_Many_Files_Reports_The_Singular_Or_Plural_Selection_Contract(int maximumFileCount, int selectedCount, string expectedMessage) {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var model = new TestModel();
        NTFileUploadEventArgs? error = null;
        var cut = RenderFileUpload(model, parameters => parameters
            .Add(p => p.MaximumFileCount, maximumFileCount)
            .Add(p => p.Multiple, true)
            .Add(p => p.OnFileError, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, args => error = args)));
        var files = Enumerable.Range(1, selectedCount)
            .Select(index => InputFileContent.CreateFromText(index.ToString(), $"file-{index}.txt"))
            .ToArray();

        cut.FindComponent<InputFile>().UploadFiles(files);

        model.Files.Should().BeNull();
        error!.ErrorMessage.Should().Be(expectedMessage);
        cut.Find(".nt-input-error-text").TextContent.Should().Be(expectedMessage);
        cut.FindAll(".nt-file-upload-item").Should().BeEmpty();
    }

    [Fact]
    public void Multiple_Oversized_Files_Report_A_Plural_Kilobyte_Limit_And_Hide_Percentages() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.MaximumFileCount, 2)
            .Add(p => p.MaximumFileSize, 1024)
            .Add(p => p.Multiple, true));
        var content = new byte[1025];

        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromBinary(content, "first.bin"),
            InputFileContent.CreateFromBinary(content, "second.bin"));

        cut.Find(".nt-input-error-text").TextContent.Should().Be("2 files are too large. Maximum file size is 1 KB.");
        cut.FindAll(".nt-file-upload-item").Should().HaveCount(2);
        cut.FindAll(".nt-file-upload-percent").Should().BeEmpty();
    }

    [Fact]
    public void Megabyte_Limit_Is_Formatted_In_The_Public_Validation_Message() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var cut = RenderFileUpload(configure: parameters => parameters.Add(p => p.MaximumFileSize, 1024 * 1024));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[(1024 * 1024) + 1], "large.bin"));

        cut.Find(".nt-input-error-text").TextContent.Should().Be("large.bin is too large. Maximum file size is 1 MB.");
    }

    [Fact]
    public void AutoUpload_Invokes_The_Handler_And_Completion_After_Selection() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var uploaded = new List<string>();
        IReadOnlyList<NTFileUploadEventArgs>? completed = null;
        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.AutoUpload, true)
            .Add(p => p.OnUploadFile, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, async args => {
                uploaded.Add(args.Name);
                await args.Stream!.CopyToAsync(Stream.Null);
            }))
            .Add(p => p.OnCompleted, EventCallback.Factory.Create<IReadOnlyList<NTFileUploadEventArgs>>(this, args => completed = args)));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("payload", "auto.txt"));

        cut.WaitForAssertion(() => {
            uploaded.Should().Equal("auto.txt");
            completed.Should().ContainSingle(file => file.Name == "auto.txt" && file.ProgressTitle == "Complete");
            cut.Find(".nt-file-upload-status").TextContent.Should().Be("Complete");
        });
    }

    [Fact]
    public void AutoUpload_Without_A_Handler_Leaves_The_File_Ready_For_The_Caller() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var model = new TestModel();
        var cut = RenderFileUpload(model, parameters => parameters.Add(p => p.AutoUpload, true));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("payload", "ready.txt"));

        model.Files.Should().ContainSingle(file => file.Name == "ready.txt");
        cut.Find(".nt-file-upload-status").TextContent.Should().Contain("Ready");
    }

    [Fact]
    public void Cancelled_Upload_Stops_Subsequent_Files_And_Still_Reports_The_Batch() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var uploaded = new List<string>();
        IReadOnlyList<NTFileUploadEventArgs>? completed = null;
        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.MaximumFileCount, 2)
            .Add(p => p.Multiple, true)
            .Add(p => p.OnUploadFile, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, args => {
                uploaded.Add(args.Name);
                args.IsCancelled = true;
            }))
            .Add(p => p.OnCompleted, EventCallback.Factory.Create<IReadOnlyList<NTFileUploadEventArgs>>(this, args => completed = args)));

        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("first", "first.txt"),
            InputFileContent.CreateFromText("second", "second.txt"));
        cut.Find(".nt-file-upload-action").Click();

        cut.WaitForAssertion(() => {
            uploaded.Should().Equal("first.txt");
            completed.Should().HaveCount(2);
            completed![0].IsCancelled.Should().BeTrue();
            cut.FindAll(".nt-file-upload-status").Select(status => status.TextContent).Should().Equal("Canceled", "Ready0%");
        });
    }

    [Fact]
    public void Upload_Failure_Renders_Failed_State_And_Emits_Error_And_Completion_Contracts() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        NTFileUploadEventArgs? error = null;
        IReadOnlyList<NTFileUploadEventArgs>? completed = null;
        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.OnUploadFile, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, _ => Task.FromException(new InvalidOperationException("upload failed"))))
            .Add(p => p.OnFileError, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, args => error = args))
            .Add(p => p.OnCompleted, EventCallback.Factory.Create<IReadOnlyList<NTFileUploadEventArgs>>(this, args => completed = args)));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("payload", "failure.txt"));
        cut.Find(".nt-file-upload-action").Click();

        cut.WaitForAssertion(() => {
            error.Should().NotBeNull();
            error!.ErrorMessage.Should().Be("upload failed");
            error.Stream.Should().BeNull();
            completed.Should().ContainSingle(file => file.Name == "failure.txt" && file.ErrorMessage == "upload failed");
            cut.Find(".nt-file-upload-status").TextContent.Should().Contain("Failed");
        });
    }

    [Fact]
    public void Reading_The_Upload_Stream_Emits_Progress_Processing_And_Completion_States() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var progress = new List<NTFileUploadProgressDetails>();
        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.OnUploadFile, EventCallback.Factory.Create<NTFileUploadEventArgs>(this, async args => {
                var buffer = new byte[4];
                (await args.Stream!.ReadAsync(buffer)).Should().Be(4);
                await args.Stream.CopyToAsync(Stream.Null);
            }))
            .Add(p => p.OnProgressChanged, EventCallback.Factory.Create<NTFileUploadProgressDetails>(this, details => progress.Add(details))));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("0123456789", "progress.txt"));
        cut.Find(".nt-file-upload-action").Click();

        cut.WaitForAssertion(() => {
            progress.Should().Contain(details => details.Status == "Uploading" && details.Percent == 40 && !details.IsIndeterminate);
            progress.Should().Contain(details => details.Status == "Processing..." && details.Percent == 100 && details.IsIndeterminate);
            progress.Should().Contain(details => details.Status == "Complete" && details.Percent == 100 && !details.IsIndeterminate);
            cut.Find(".nt-file-upload-status").TextContent.Should().Be("Complete");
        });
    }

    [Fact]
    public void Progress_Template_Receives_The_Selected_File_Details() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var cut = RenderFileUpload(configure: parameters => parameters
            .Add(p => p.ProgressTemplate, details => builder => {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-file-progress");
                builder.AddContent(2, $"{details.Name}|{details.Size}|{details.Status}");
                builder.CloseElement();
            }));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("data", "template.txt"));

        cut.Find(".custom-file-progress").TextContent.Should().Be("template.txt|4|Ready");
    }

    private IRenderedComponent<NTFileUpload> RenderFileUpload(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTFileUpload>>? configure = null) {
        model ??= new TestModel();
        return Render<NTFileUpload>(parameters => {
            parameters
                .Add(p => p.Value, model.Files)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>?>(this, value => model.Files = value))
                .Add(p => p.ValueExpression, (Expression<Func<IReadOnlyList<IBrowserFile>?>>)(() => model.Files));
            configure?.Invoke(parameters);
        });
    }

    private sealed class FakeBrowserFile(string name, long size) : IBrowserFile {
        public DateTimeOffset LastModified { get; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public string Name { get; } = name;

        public long Size { get; } = size;

        public string ContentType { get; } = "text/plain";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) {
            if (Size > maxAllowedSize) {
                throw new IOException($"Supplied file with size {Size} bytes exceeds the maximum of {maxAllowedSize} bytes.");
            }

            return new MemoryStream(new byte[(int)Size]);
        }
    }
}
