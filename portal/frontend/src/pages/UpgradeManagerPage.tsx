import './UpgradeManagerPage.css';

export const UpgradeManagerPage = () => {
  return (
    <section className="upgrade-manager-page">
      <header>
        <div>
          <p className="eyebrow">System Management</p>
          <h1>Upgrade Manager</h1>
          <p>Manage module version upgrades across client applications</p>
        </div>
      </header>

      <div className="empty-state">
        <h2>Coming Soon</h2>
        <p>
          This feature will allow admins to orchestrate bulk module upgrades,
          track breaking changes, and notify clients about available updates.
        </p>
      </div>
    </section>
  );
};
