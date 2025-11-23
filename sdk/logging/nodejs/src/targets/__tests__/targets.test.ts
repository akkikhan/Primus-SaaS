import { ConsoleTarget } from '../ConsoleTarget';
import { LogLevel } from '../../core/LogLevel';
import { createLogEntry } from '../../core/LogEntry';

describe('Targets', () => {
    describe('ConsoleTarget', () => {
        let consoleSpy: jest.SpyInstance;

        beforeEach(() => {
            consoleSpy = jest.spyOn(console, 'log').mockImplementation();
        });

        afterEach(() => {
            consoleSpy.mockRestore();
        });

        it('should write JSON logs by default', () => {
            const target = new ConsoleTarget();
            const entry = createLogEntry(LogLevel.INFO, 'Test message', { foo: 'bar' });

            target.write(entry);

            expect(consoleSpy).toHaveBeenCalledWith(JSON.stringify(entry));
        });

        it('should pretty print logs when configured', () => {
            const target = new ConsoleTarget({ pretty: true });
            const entry = createLogEntry(LogLevel.INFO, 'Test message', { foo: 'bar' });

            target.write(entry);

            const output = consoleSpy.mock.calls[0][0];
            expect(output).toContain('INFO');
            expect(output).toContain('Test message');
        });
    });

    // FileTarget tests are skipped due to environment-specific file I/O issues
    // File logging functionality is thoroughly tested in integration.test.ts
});
