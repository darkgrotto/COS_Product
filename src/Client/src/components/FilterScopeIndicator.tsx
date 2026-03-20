import { useDemo } from '../context/DemoContext';

export function FilterScopeIndicator() {
  const { isDemo, demoSets } = useDemo();
  if (!isDemo || demoSets.length === 0) return null;

  const setList = demoSets.map((s) => s.toUpperCase()).join(', ');
  return (
    <p role="note">
      Demo environment: results limited to demo sets ({setList}).
    </p>
  );
}
