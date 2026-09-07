using DndCompanion.Views;

namespace DndCompanion;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(DocumentPage), typeof(DocumentPage));
		Routing.RegisterRoute(nameof(StoryPage), typeof(StoryPage));
		Routing.RegisterRoute(nameof(CharactersPage), typeof(CharactersPage));
		Routing.RegisterRoute(nameof(CharacterDetailPage), typeof(CharacterDetailPage));
	}
}
